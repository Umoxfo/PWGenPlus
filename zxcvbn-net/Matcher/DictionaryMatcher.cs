using System;
using System.Collections.Generic;
using System.Linq;

namespace Zxcvbn.Matcher
{
    /// <summary>
    /// <para>This matcher reads in a list of words (in frequency order) and matches substrings of the password against that dictionary.</para>
    ///
    /// <para>The dictionary to be used can be specified directly by passing an enumerable of strings through the constructor (e.g. for matching again user inputs). Most dictionaries will be in word list files.</para>
    ///
    /// <para>Using external files is a departure from the JS version of Zxcvbn which bakes in the word lists,
    /// so the default dictionaries have been included in the Zxcvbn assembly as embedded resources
    /// (to remove the external dependency).
    /// Thus when a word list is specified by name, it is first checked to see if it matches and embedded resource
    /// and if not is assumed to be an external file. </para>
    ///
    /// <para>Thus custom dictionaries can be included by providing the name of an external text file,
    /// but the built-in dictionaries (english, female_names, male_names, passwords, surnames) can be used without concern
    /// about locating a dictionary file in an accessible place.</para>
    ///
    /// <para>Dictionary word lists must be in decreasing frequency order and contain one word per line with no additional information.</para>
    /// </summary>
    public class DictionaryMatcher : IMatcher
    {
        private readonly Dictionary<string, Dictionary<string, int>> rankedDictionaries;

        public DictionaryMatcher(in Dictionary<string, Dictionary<string, int>> rankedDictionaries = null) =>
            this.rankedDictionaries = rankedDictionaries ?? new Dictionary<string, Dictionary<string, int>>();

        /// <summary>
        /// Match substrings of password with the loaded dictionary
        /// </summary>
        /// <param name="password">The password to match</param>
        /// <returns>An enumerable of dictionary matches</returns>
        /// <seealso cref="DictionaryMatch"/>
        public IEnumerable<Match> MatchPassword(string password)
        {
            if (string.IsNullOrEmpty(password)) throw new ArgumentNullException(nameof(password));

            return from dictionary in rankedDictionaries

                   from i in Enumerable.Range(0, password.Length)
                   from j in Enumerable.Range(i, password.Length - i)
                   let word = password.ToLowerInvariant().Substring(i, j - i + 1)
                   where dictionary.Value.ContainsKey(word)

                   let token = password.Substring(i, j - i + 1) // Could have different case so pull from password
                   let rank = dictionary.Value[word]
                   let baseEntropy = Math.Log(rank, 2)
                   let uppercaseEntropy = PasswordScoring.CalculateUppercaseEntropy(token)
                   select new DictionaryMatch
                   {
                       Pattern = Pattern.Dictionary,
                       i = i,
                       j = j,
                       Token = token,
                       MatchedWord = word,
                       Rank = rank,
                       DictionaryName = dictionary.Key,
                       Reversed = false,
                       L33t = false,
                       Cardinality = dictionary.Value.Count,

                       //Calculate entropy
                       BaseEntropy = baseEntropy,
                       UppercaseEntropy = uppercaseEntropy,
                       Entropy = baseEntropy + uppercaseEntropy
                   } into dm

                   orderby dm
                   select dm;
        }//MatchPassword
    }

    /// <summary>
    /// This matcher matches substrings of <b>reversed</b> password with the loaded dictionary
    /// </summary>
    /// <seealso cref="DictionaryMatcher"/>
    public class ReversedDictionaryMatcher : IMatcher
    {
        private readonly DictionaryMatcher dictionaryMatcher;

        public ReversedDictionaryMatcher(in DictionaryMatcher dictionaryMatcher = null) =>
            this.dictionaryMatcher = dictionaryMatcher ?? new DictionaryMatcher();

        /// <summary>
        /// Match substrings of reversed password with the loaded dictionary
        /// </summary>
        /// <param name="password">The password to match</param>
        /// <returns>An enumerable of reversed dictionary matches</returns>
        /// <seealso cref="DictionaryMatch"/>
        public IEnumerable<Match> MatchPassword(string password)
        {
            if (string.IsNullOrEmpty(password)) throw new ArgumentNullException(nameof(password));

            IEnumerable<DictionaryMatch> matches = dictionaryMatcher.MatchPassword(string.Join("", password.Reverse())).OfType<DictionaryMatch>();
            foreach (var match in matches)
            {
                match.Token = string.Join("", match.Token.Reverse()); // Reverse back
                match.Reversed = true;
                // Map coordinates back to original string
                (match.i, match.j) = (password.Length - 1 - match.j, password.Length - 1 - match.i);
            }

            return matches.OrderBy(m => m).ToList();
        }//MatchPassword
    }//ReversedDictionaryMatcher

    /// <summary>
    /// A match made with the <see cref="DictionaryMatcher"/>
    /// that contains some additional information about the matched word.
    /// </summary>
    public class DictionaryMatch : Match
    {
        /// <summary>
        /// The dictionary word matched
        /// </summary>
        public string MatchedWord { get; set; }

        /// <summary>
        /// The rank of the matched word in the dictionary (i.e. 1 is most frequent, and larger numbers are less common words)
        /// </summary>
        public int Rank { get; set; }

        /// <summary>
        /// The name of the dictionary the matched word was found in
        /// </summary>
        public string DictionaryName { get; set; }

        public bool Reversed { get; set; } = false;

        public bool L33t { get; set; } = false;

        /// <summary>
        /// The base entropy of the match, calculated from frequency rank
        /// </summary>
        public double BaseEntropy { get; set; }

        /// <summary>
        /// Additional entropy for this match from the use of mixed case
        /// </summary>
        public double UppercaseEntropy { get; set; }
    }
}
