using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

using Umoxfo.Zxcvbn.Report;

namespace Umoxfo.Zxcvbn.Matcher
{
    /// <summary>
    /// Match repeated characters in the password (repeats must be more than two characters long to count)
    /// </summary>
    public class RepeatMatcher : IMatcher
    {
        private const RegexOptions options = RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.IgnoreCase;

        private readonly Regex greedy = new Regex(@"(.+)\1+", options);
        private readonly Regex lazy = new Regex(@"(.+?)\1+", options);
        private readonly Regex lazyAnchored = new Regex(@"^(.+?)\1+$", options);

        /// <summary>
        /// Find repeat matches in <paramref name="password"/>
        /// </summary>
        /// <param name="password">The password to check</param>
        /// <returns>List of repeat matches</returns>
        /// <seealso cref="RepeatMatch"/>
        public IEnumerable<Match> MatchPassword(string password)
        {
            if (string.IsNullOrEmpty(password)) throw new ArgumentNullException(nameof(password));

            int lastIndex = 0;
            while (lastIndex < password.Length)
            {
                if (!greedy.IsMatch(password, lastIndex)) break;

                var greedyMatch = greedy.Match(password, lastIndex);
                var lazyMatch = lazy.Match(password, lastIndex);

                System.Text.RegularExpressions.Match match;
                string baseToken;
                if (greedyMatch.Length > lazyMatch.Length)
                {
                    // greedy beats lazy for 'aabaab'
                    //   greedy: [aabaab, aab]
                    //   lazy:   [aa,     a]
                    match = greedyMatch;

                    // Greedy's repeated string might itself be repeated, e.g. aabaab in aabaabaabaab.
                    // Run an anchored lazy match on greedy's repeated string to find the shortest repeated string
                    baseToken = lazyAnchored.IsMatch(match.Value) ? lazyAnchored.Match(match.Value).Groups[1].Value : match.Value;
                }
                else
                {
                    // lazy beats greedy for 'aaaaa'
                    //   greedy: [aaaa,  aa]
                    //   lazy:   [aaaaa, a]
                    match = lazyMatch;
                    baseToken = match.Groups[1].Value;
                }//if-else

                int i = match.Index;
                int j = match.Index + (match.Length - 1);
                Result baseAnalysis = Scoring.MostGuessableMatchSequence(baseToken, new DefaultMatcherFactory().Omnimatch(baseToken));
                int repeatCount = match.Value.Length / baseToken.Length;

                yield return new RepeatMatch(match.Value.Length, password.Length)
                {
                    Pattern = Pattern.Repeat,
                    i = i,
                    j = j,
                    Token = match.Value,
                    BaseToken = baseToken,
                    BaseGuesses = baseAnalysis.Guesses,
                    BaseMatches = baseAnalysis.GuessesSequence,
                    RepeatCount = repeatCount,

                    Entropy = CalculateEntropy(password.Substring(i, j - i + 1)),
                    Guesses = baseAnalysis.Guesses * repeatCount
                };

                lastIndex = i + j;
            }//while
        }

        private double CalculateEntropy(string match) => Math.Log(PasswordScoring.PasswordCardinality(match) * match.Length, 2);
    }

    /// <summary>
    /// A match made with the <see cref="RepeatMatcher"/> that contains some additional information specific to the repeat match.
    /// </summary>
    public class RepeatMatch : Match
    {
        public RepeatMatch(int tokenLength, int passwordLength) : base(tokenLength, passwordLength)
        {
        }

        public string BaseToken { get; set; }

        public double BaseGuesses { get; set; }

        public IEnumerable<Match> BaseMatches { get; set; }

        /// <summary>
        /// The number of repeated
        /// </summary>
        /// <value>The number of repeated</value>
        public int RepeatCount { get; set; }
    }
}
