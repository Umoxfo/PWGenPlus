using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace Zxcvbn.Matcher
{
    /// <summary>
    /// This matcher applies some known l33t character substitutions and then attempts to match against passed in dictionary matchers.
    /// This detects passwords like 4pple which has a '4' substituted for an 'a'
    /// </summary>
    public class L33tMatcher : IMatcher
    {
        private static readonly ReadOnlyDictionary<char, string> L33tTable =
            new ReadOnlyDictionary<char, string>(new Dictionary<char, string>()
            {
                ['a'] = "4@",
                ['b'] = "8",
                ['c'] = "({[<",
                ['e'] = "3",
                ['g'] = "69",
                ['i'] = "1!|",
                ['l'] = "1|7",
                ['o'] = "0",
                ['s'] = "$5",
                ['t'] = "+7",
                ['x'] = "%",
                ['z'] = "2"
            });

        private readonly DictionaryMatcher dictionaryMatcher;

        /// <summary>
        /// Create a l33t matcher that applies substitutions and then matches with the passed in dictionary.
        /// </summary>
        /// <param name="rankedDictionaries">The dictionary to check transformed passwords against</param>
        public L33tMatcher(in DictionaryMatcher dictionaryMatcher) =>
            this.dictionaryMatcher = dictionaryMatcher ?? new DictionaryMatcher();

        /// <summary>
        /// Apply applicable l33t transformations and check <paramref name="password"/> against the dictionaries.
        /// </summary>
        /// <param name="password">The password to match</param>
        /// <returns>A list of match objects where l33t substitutions match dictionary words</returns>
        /// <seealso cref="L33tDictionaryMatch"/>
        public IEnumerable<Match> MatchPassword(string password)
        {
            if (string.IsNullOrEmpty(password)) throw new ArgumentNullException(nameof(password));

            List<Dictionary<char, char>> subs = EnumerateSubtitutions(GetRelevantSubstitutions(password));

            var matches =
                from subDict in subs
                let sub_password = TranslateString(password, subDict)
                from match in dictionaryMatcher.MatchPassword(sub_password).OfType<DictionaryMatch>()
                let token = password.Substring(match.i, match.j - match.i + 1)
                let matchSub = subDict.Where(kv => token.Contains(kv.Key)) // Count subs used in matched token

                // Matches only when substitution is used and
                // filters single-letter l33t matches to reduce noise
                // from very common English words with low dictionary rank
                // (e.g. '1' matches 'i', '4' matches 'a').
                where matchSub.Any() && (token.Length > 1)
                select new L33tDictionaryMatch(match)
                {
                    Token = token,
                    Subs = matchSub.ToDictionary(kv => kv.Key, kv => kv.Value),
                    SubDisplay = string.Join(", ", matchSub.Select(kv => $"{kv.Key} -> {kv.Value}"))
                };

            foreach (L33tDictionaryMatch match in matches) CalulateL33tEntropy(match);

            return matches.OrderBy(m => m);
        }//MatchPassword

        /// <summary>
        /// Return a map of the useful substitutions that only includes possible substitutions of the password.
        /// </summary>
        /// <param name="password">The password to check</param>
        /// <param name="table">The substitution table</param>
        /// <returns>A map of the possible substitutions of the password</returns>
        private Dictionary<char, string> GetRelevantSubstitutions(string password, IDictionary<char, string> table = null)
        {
            if (table == null) table = L33tTable;

            return (from t in table
                    let relevantSubs = t.Value.Intersect(password)
                    where relevantSubs.Any()
                    select (t.Key, Value: string.Join("", relevantSubs))
                   ).ToDictionary(st => st.Key, st => st.Value);
        }//GetRelevantSubstitutions

        private List<Dictionary<char, char>> EnumerateSubtitutions(Dictionary<char, string> table)
        {
            // Produce a list of maps from l33t character to normal character.
            // Some substitutions can be more than one normal character though,
            // so we have to produce an entry that maps from the l33t char to both possibilities

            //XXX: This function produces different combinations to the original in zxcvbn.
            // It may require some more work to get identical.

            //XXX: The function is also limited in that it only ever considers one substitution for each l33t character
            // (e.g. ||ke could feasibly match 'like' but this method would never show this).
            // My understanding is that this is also a limitation in zxcvbn and so I feel no need to correct it here.

            List<Dictionary<char, char>> subs = new List<Dictionary<char, char>>
            {
                new Dictionary<char, char>() // Must be at least one mapping dictionary to work
            };

            foreach (var mapPair in table)
            {
                char normalChar = mapPair.Key;

                foreach (char l33tChar in mapPair.Value)
                {
                    // Can't add while enumerating so store here
                    List<Dictionary<char, char>> addedSubs = new List<Dictionary<char, char>>();

                    foreach (Dictionary<char, char> subDict in subs)
                    {
                        if (subDict.ContainsKey(l33tChar))
                        {
                            // This mapping already contains a corresponding normal character for this character,
                            // so keep the existing one as is but add a duplicate with the mapping replaced with this normal character
                            addedSubs.Add(new Dictionary<char, char>(subDict)
                            {
                                [l33tChar] = normalChar
                            });
                        }
                        else
                        {
                            subDict[l33tChar] = normalChar;
                        }
                    }

                    subs.AddRange(addedSubs);
                }
            }

            return subs;
        }

        // Make substitutions from the character map wherever possible
        private string TranslateString(string str, Dictionary<char, char> charMap) =>
            string.Join("", str.Select(c => charMap.TryGetValue(c, out char cv) ? cv : c));

        private void CalulateL33tEntropy(in L33tDictionaryMatch match)
        {
            int possibilities = 0;

            foreach (var kvp in match.Subs)
            {
                int subbedChars = match.Token.Where(c => c == kvp.Key).Count();
                int unsubbedChars = match.Token.Where(c => c == kvp.Value).Count(); // Won't this always be zero?

                possibilities += Enumerable.Range(0, Math.Min(subbedChars, unsubbedChars) + 1)
                                           .Sum(i => (int)PasswordScoring.Binomial(subbedChars + unsubbedChars, i));
            }

            double entropy = Math.Log(possibilities, 2);

            // In the case of only a single substitution (e.g. 4pple) this would otherwise come out as zero, so give it one bit
            match.L33tEntropy = (entropy < 1 ? 1 : entropy);
            match.Entropy += match.L33tEntropy;

            // We have to recalculate the uppercase entropy
            // - the password matcher will have used the subbed password not the original text
            match.Entropy -= match.UppercaseEntropy;
            match.UppercaseEntropy = PasswordScoring.CalculateUppercaseEntropy(match.Token);
            match.Entropy += match.UppercaseEntropy;
        }
    }

    /// <summary>
    /// A match result made with the <see cref="L33tMatcher"/> that contains
    /// the <see cref="DictionaryMatcher"/> and extra information related to
    /// the additional entropy obtained by using substitution.
    /// </summary>
    public class L33tDictionaryMatch : DictionaryMatch
    {
        /// <summary>
        /// The character mappings that are in use for this match
        /// </summary>
        public Dictionary<char, char> Subs { get; set; }

        public string SubDisplay { get; set; } = string.Empty;

        /// <summary>
        /// The extra entropy from using l33t substitutions
        /// </summary>
        public double L33tEntropy { get; set; }

        /// <summary>
        /// Create a new l33t match from a dictionary match
        /// </summary>
        /// <param name="dm">The dictionary match to initialize the l33t match from</param>
        public L33tDictionaryMatch(DictionaryMatch dm)
        {
            if (dm is null) dm = new DictionaryMatch();

            Pattern = dm.Pattern;
            i = dm.i;
            j = dm.j;
            MatchedWord = dm.MatchedWord;
            Token = dm.Token;
            Rank = dm.Rank;
            DictionaryName = dm.DictionaryName;
            Reversed = dm.Reversed;
            L33t = true;

            Cardinality = dm.Cardinality;
            BaseEntropy = dm.BaseEntropy;
            UppercaseEntropy = dm.UppercaseEntropy;
            Entropy = dm.Entropy;
        }
    }

    //private class SubtitutionsEnumerator
    //{
    //    private readonly Dictionary<char, string> table;

    //    private List<Dictionary<char, char>> subs;

    //    internal SubtitutionsEnumerator(Dictionary<char, string> table)
    //    {
    //        this.table = table;
    //        subs = new List<Dictionary<char, char>>
    //        {
    //            new Dictionary<char, char>() // Must be at least one mapping dictionary to work
    //        };
    //    }

    //    internal List<Dictionary<char, char>> EnumerateSubtitutions(Dictionary<char, string> table)
    //    {
    //        // Produce a list of maps from l33t character to normal character.
    //        // Some substitutions can be more than one normal character though,
    //        // so we have to produce an entry that maps from the l33t char to both possibilities

    //        //XXX: This function produces different combinations to the original in zxcvbn.
    //        // It may require some more work to get identical.

    //        //XXX: The function is also limited in that it only ever considers one substitution for each l33t character
    //        // (e.g. ||ke could feasibly match 'like' but this method would never show this).
    //        // My understanding is that this is also a limitation in zxcvbn and so I feel no need to correct it here.

    //        List<Dictionary<char, char>> subs = new List<Dictionary<char, char>>
    //        {
    //            new Dictionary<char, char>() // Must be at least one mapping dictionary to work
    //        };

    //        foreach (var mapPair in table)
    //        {
    //            char normalChar = mapPair.Key;

    //            foreach (char l33tChar in mapPair.Value)
    //            {
    //                // Can't add while enumerating so store here
    //                List<Dictionary<char, char>> addedSubs = new List<Dictionary<char, char>>();

    //                foreach (Dictionary<char, char> subDict in subs)
    //                {
    //                    if (subDict.ContainsKey(l33tChar))
    //                    {
    //                        // This mapping already contains a corresponding normal character for this character,
    //                        // so keep the existing one as is but add a duplicate with the mapping replaced with this normal character
    //                        Dictionary<char, char> newSub = new Dictionary<char, char>(subDict);
    //                        newSub[l33tChar] = normalChar;
    //                        addedSubs.Add(newSub);
    //                    }
    //                    else
    //                    {
    //                        subDict[l33tChar] = normalChar;
    //                    }
    //                }

    //                subs.AddRange(addedSubs);
    //            }
    //        }

    //        return subs;
    //    }

    //    private void Helper(IEnumerable<char> keys)
    //    {
    //        if (!keys.Any()) return;

    //        char firstKey = keys.First();
    //        char[] restKeys = keys.Skip(1).ToArray();
    //        List<Dictionary<char, char>> nextSubs = new List<Dictionary<char, char>>
    //        {
    //            new Dictionary<char, char>()
    //        };
    //        foreach (char l33tChr in table[firstKey])
    //        {
    //            foreach (var sub in subs)
    //            {
    //                int dupL33tIndex = -1;

    //            }
    //        }
    //    }
    //}
}
