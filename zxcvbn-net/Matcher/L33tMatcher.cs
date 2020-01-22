using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;

namespace Umoxfo.Zxcvbn.Matcher
{
    /// <summary>
    /// This matcher applies some known l33t character substitutions and then attempts to match against passed in dictionary matchers.
    /// This detects passwords like 4pple which has a '4' substituted for an 'a'
    /// </summary>
    public class L33tMatcher : IMatcher
    {
        // Source: Leet speak alphabet by Roald Craenen (http://www.gamehouse.com/blog/leet-speak-cheat-sheet/)
        private static readonly ReadOnlyDictionary<char, string> L33tTable =
            new ReadOnlyDictionary<char, string>(new Dictionary<char, string>()
            {
                ['a'] = "4@^Д",
                ['b'] = "8ß6",
                ['c'] = "[¢{<(©",
                ['d'] = ")?>",
                ['e'] = "3&£€ë",
                ['f'] = "ƒv",
                ['g'] = "&69",
                ['h'] = "#",
                ['i'] = "1!|",
                ['j'] = "];1",
                ['l'] = "1£7|",
                ['n'] = "И^ท",
                ['o'] = "0QpØ",
                ['p'] = "09",
                ['q'] = "92&",
                ['r'] = "2®Я",
                ['s'] = "$5z§2",
                ['t'] = "7+†",
                ['u'] = "vµบ",
                ['w'] = "ШЩพ",
                ['x'] = "Ж×?%",
                ['y'] = "jЧ7¥",
                ['z'] = "2%s"
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

            var matches =
                from subDict in EnumerateL33tSubs(GetRelevantSubstitutions(password))
                from sub_password in TranslateString(password, subDict)
                from match in dictionaryMatcher.MatchPassword(sub_password).OfType<DictionaryMatch>()
                let token = password.Substring(match.i, match.j - match.i + 1)
                let matchSub = subDict.Where(kv => token.Contains(kv.Key, StringComparison.OrdinalIgnoreCase)) // Count subs used in matched token

                // Matches only when substitution is used and
                // filters single-letter l33t matches to reduce noise
                // from very common English words with low dictionary rank
                // (e.g. '1' matches 'i', '4' matches 'a').
                where matchSub.Any() && (token.Length > 1)
                let uppercaseVariations = PasswordScoring.CalculateUppercaseVariations(token)
                let l33tVariations = CalculateL33tVariations(matchSub, token)
                select new L33tDictionaryMatch(match, password.Length)
                {
                    Token = token,
                    Subs = new ReadOnlyDictionary<char, char>(matchSub.ToDictionary(kv => kv.Key, kv => kv.Value)),
                    SubDisplay = string.Join(", ", matchSub.Select(kv => $"{kv.Key} -> {kv.Value}")),

                    UppercaseEntropy = uppercaseVariations.Entropy,
                    L33tEntropy = l33tVariations.Entropy,
                    Guesses = match.Rank * uppercaseVariations.Guesses * l33tVariations.Guesses
                };

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
            return (from t in table ?? L33tTable
                    let relevantSubs = t.Value.Intersect(password)
                    where relevantSubs.Any()
                    select (t.Key, Value: string.Concat(relevantSubs))
                   ).ToDictionary(st => st.Key, st => st.Value);
        }//GetRelevantSubstitutions

        private List<Dictionary<char, char>> EnumerateL33tSubs(Dictionary<char, string> table)
        {
            // Produce a list of maps from l33t character to normal character.
            // Some substitutions can be more than one normal character though,
            // so we have to produce an entry that maps from the l33t char to both possibilities

            CompareSub compareSub = new CompareSub();
            List<List<char[]>> subs = new List<List<char[]>> { new List<char[]>() };

            Helper(table.Keys);

            return subs.Select(sub => sub.ToDictionary(refr => refr[0], refr => refr[1])).ToList();

            IEnumerable<List<char[]>> Dedup(List<List<char[]>> tmpSubs)
            {
                HashSet<string> members = new HashSet<string>();
                foreach (var sub in tmpSubs)   //[l33tChr, firstKey] of "sub" array
                {
                    var assoc = sub.Select((val, i) => (Key: val, Value: i))
                                   .OrderBy(kv => kv, compareSub)
                                   .Select((kv, i) => $"{string.Join(",", kv.Key)},{kv.Value},{i}");
                    string label = string.Join("-", assoc);

                    #region original
                    //Dictionary<int, char[]> assoc = new Dictionary<int, char[]>();
                    //for (int i = 0; i < sub.Count; i++)
                    //{
                    //    assoc[i] = sub[i];
                    //}

                    //List<string> strings = new List<string>();
                    //foreach (var entry in assoc)
                    //{
                    //    strings.Add($"{string.Concat( entry.Value)},{entry.Key}");
                    //}

                    //StringBuilder builder = new StringBuilder();
                    //foreach (string str in strings)
                    //{
                    //    builder.Append(str).Append("-");
                    //}
                    //string label = builder.ToString().Substring(0, builder.Length - 1);
                    #endregion

                    if (members.Add(label)) yield return sub;
                }//foreach subs
            }//Dedup

            void Helper(IEnumerable<char> keys)
            {
                if (!keys.Any()) return;

                char firstKey = keys.FirstOrDefault();
                List<char> restKeys = keys.Skip(1).ToList();

                List<List<char[]>> nextSubs = new List<List<char[]>>();
                foreach (var l33tChr in table.TryGetValue(firstKey, out string str) ? str : string.Empty)
                {
                    foreach (var sub in subs)
                    {
                        int dupL33tIndex = -1;
                        for (int i = 0; i < sub.Count; i++)
                        {
                            if (sub[i][0] == l33tChr)
                            {
                                dupL33tIndex = i;
                                break;
                            }
                        }

                        if (dupL33tIndex == -1)
                        {
                            List<char[]> subExtension = new List<char[]>(sub)
                            {
                                new char[] { l33tChr, firstKey }
                            };
                            nextSubs.Add(subExtension);
                        }
                        else
                        {
                            List<char[]> subAlternative = new List<char[]>(sub);
                            subAlternative.RemoveAt(dupL33tIndex);
                            subAlternative.Add(new char[] { l33tChr, firstKey });
                            nextSubs.Add(sub);
                            nextSubs.Add(subAlternative);
                        }//if-else
                    }//foreach subs
                }//foreach table[firstKey]

                subs = Dedup(nextSubs).ToList();

                Helper(restKeys.ToArray());
            }//Helper
        }//EnumerateL33tSubs

        // Make substitutions from the character map wherever possible
        private IEnumerable<string> TranslateString(string str, Dictionary<char, char> charMap)
        {
            var repTable = str.Select((c, index) => (index, ltCh: c, rpCh: charMap.TryGetValue(c, out char cv) ? cv : c))
                              .Where(c => c.ltCh != c.rpCh).ToList();

            int i = 1;
            foreach (var (index, ltCh, rpCh) in repTable)
            {
                StringBuilder sb = new StringBuilder(str);
                yield return sb.Replace(ltCh, rpCh, index, 1).ToString();

                for (int j = i; j < repTable.Count; j++)
                {
                    var sbNew = new StringBuilder(sb.ToString(), sb.Length);
                    foreach (var rep in repTable.Skip(j))
                    {
                        yield return sbNew.Replace(rep.ltCh, rep.rpCh, rep.index, 1).ToString();
                    }
                }//for

                i++;
            }//foreach repTable (Whole)
        }//TranslateString

        private (double Entropy, long Guesses) CalculateL33tVariations(IEnumerable<KeyValuePair<char, char>> matchSubs, string token)
        {
            long possibilities = 0;
            long variations = 1;

            foreach (var subRef in matchSubs)
            {
                int subbedChars = 0;
                int unsubbedChars = 0;
                // Lowercase match.Token before calculating: capitalization shouldn't affect l33t calculate
                foreach (char chr in token.ToLowerInvariant())
                {
                    if (chr == subRef.Key) subbedChars++;
                    if (chr == subRef.Value) unsubbedChars++;
                }

                long tmp = Enumerable.Range(1, Math.Min(unsubbedChars, subbedChars))
                                     .Sum(i => PasswordScoring.Binomial(unsubbedChars + subbedChars, i));

                // Entropy
                possibilities += tmp + 1;

                // Guesses
                if (subbedChars == 0 || unsubbedChars == 0)
                {
                    // For this sub, password is either fully subbed (444) or fully unsubbed (aaa)
                    // treat that as doubling the space (attacker needs to try fully subbed chars
                    // in addition to unsubbed.)
                    variations *= 2;
                }
                else
                {
                    // This case is similar to capitalization:
                    // with aa44a, U = 3, S = 2, attacker needs to try unsubbed + one sub + two subs
                    variations *= tmp;
                }
            }//foreach

            double entropy = Math.Log(possibilities, 2);

            return (Entropy: (entropy < 1) ? 1 : entropy, Guesses: variations);
        }//CalculateL33tVariations

        private class CompareSub : IComparer<(char[] Key, int Value)>
        {
            public int Compare((char[] Key, int Value) x, (char[] Key, int Value) y)
            {
                int compL33tChr = x.Key[0].CompareTo(y.Key[0]);
                int compFirstKey = x.Key[1].CompareTo(y.Key[1]);

                return compL33tChr != 0 ? compL33tChr : compFirstKey;
            }
        }//CompareSub
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
        /// <value>The character mappings that are in use for this match</value>
        public IReadOnlyDictionary<char, char> Subs { get; set; }

        public string SubDisplay { get; set; } = string.Empty;

        public override double Entropy => BaseEntropy + UppercaseEntropy + L33tEntropy;

        /// <summary>
        /// The extra entropy from using l33t substitutions
        /// </summary>
        public double L33tEntropy { get; set; }

        /// <summary>
        /// Create a new l33t match from a dictionary match
        /// </summary>
        /// <param name="dm">The dictionary match to initialize the l33t match from</param>
        public L33tDictionaryMatch(DictionaryMatch dm, int passwordLength)
            : base(PassThroughNonNull(dm).Token.Length, passwordLength)
        {
            if (dm == null) dm = new DictionaryMatch(0, passwordLength);

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
        }

        // Null check method
        private static DictionaryMatch PassThroughNonNull(DictionaryMatch dm)
        {
            if (dm == null) throw new ArgumentNullException(nameof(dm));
            return dm;
        }//PassThroughNonNull
    }
}
