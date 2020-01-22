using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

using Umoxfo.Zxcvbn.Report;

namespace Umoxfo.Zxcvbn.Matcher
{
    /// <summary>
    /// <para>Use a regular expression to match against the password.
    /// (e.g. 'year' and 'digits' pattern matchers are implemented with this matcher.)</para>
    /// <para>A note about cardinality: the cardinality parameter is used to
    /// calculate the entropy of matches found with the regular expression (RegEx) matcher.
    /// Since this cannot be calculated automatically from the RegEx pattern it must be provided.
    /// It can be provided per-character or per-match.
    /// Per-match will result in every match having the same entropy (e.g. cardinality)
    /// whereas per-character will depend on the match length (e.g. cardinality ^ length)</para>
    /// </summary>
    public class RegexMatcher : IMatcher
    {
        private readonly IDictionary<string, RegexMatcherInfo> regexen;

        /// <summary>
        /// Create a new regular expression (RegEx) pattern matcher
        /// </summary>
        /// <param name="regexen">The dictionary for RegEx pattern information
        /// with a matcher name as key and <see cref="RegexMatcherInfo"/> as value</param>
        public RegexMatcher(IDictionary<string, RegexMatcherInfo> regexen = null) =>
            this.regexen = regexen ?? new Dictionary<string, RegexMatcherInfo>();

        /// <summary>
        /// Set (or add if not exist) a new regular expression (RegEx) pattern information
        /// </summary>
        /// <param name="matcherName">The name of this matcher (<see cref="RegexMatch.RxName"/>)</param>
        /// <param name="pattern">The RegEx pattern to match</param>
        /// <param name="cardinality">The cardinality of this match.
        ///   Since this is not able to be calculated from a pattern it must be provided.
        ///   It could be given per-match-character or per-match.</param>
        /// <param name="perCharCardinality">True if cardinality is given as per-matched-character</param>
        public void SetRegexn(string matcherName, string pattern, int cardinality, bool perCharCardinality = true) =>
            SetRegexn(matcherName, new Regex(pattern, RegexOptions.Compiled), cardinality, perCharCardinality);

        /// <summary>
        /// Set (or add if not exist) a new regular expression (RegEx) pattern information
        /// </summary>
        /// <param name="matcherName">The name to give this matcher (<see cref="RegexMatch.RxName"/>)</param>
        /// <param name="matchRegex">The RegEx object used to perform matching</param>
        /// <param name="cardinality">The cardinality of this match.
        ///   Since this is not able to be calculated from a pattern it must be provided.
        ///   It could be given per-match-character or per-match.</param>
        /// <param name="perCharCardinality">True if cardinality is given as per-matched-character</param>
        public void SetRegexn(string matcherName, Regex matchRegex, int cardinality, bool perCharCardinality) =>
            regexen[matcherName] = new RegexMatcherInfo(matchRegex, cardinality, perCharCardinality);

        /// <summary>
        /// Find all matches of the regular expression (RegEx) in <paramref name="password"/>
        /// </summary>
        /// <param name="password">The password to check</param>
        /// <returns>An enumerable of matches for each RegEx match in <paramref name="password"/></returns>
        public IEnumerable<Match> MatchPassword(string password)
        {
            return regexen.SelectMany(rx =>
            {
                int cardinality = rx.Value.Cardinality;

                return rx.Value.MatcherRegex.Matches(password).Cast<System.Text.RegularExpressions.Match>().Select(rem =>
                {
                    var (entropy, guesses) = CalculateVariations(rx.Key, rem.Value, cardinality, rem.Length, rx.Value.PerCharCardinality);

                    return new RegexMatch(rem.Length, password.Length)
                    {
                        Pattern = Pattern.Regex,
                        i = rem.Index,
                        j = rem.Index + (rem.Length - 1),
                        Token = password.Substring(rem.Index, rem.Length),
                        RxName = rx.Key,
                        RxMatch = rem,
                        Cardinality = cardinality,

                        Entropy = entropy,
                        Guesses = guesses
                    };
                });
            }).OrderBy(rm => rm);
        }//MatchPassword

        private static (double Entropy, double Guesses) CalculateVariations(string regexName, string regexValue, int cardinality, int tokenLength, bool perCharCardinality)
        {
            double guesses;
            switch (regexName)
            {
                case "recent_year":
                    // Conservative estimate of year space: number years from ReferenceYear.
                    // if year is close to ReferenceYear, estimate a year space of MinYearSpace.
                    guesses = Math.Max(Math.Abs(regexValue.ToInt() - Scoring.ReferenceYear), Scoring.MinYearSpace); break;
                default: guesses = Math.Pow(cardinality, tokenLength); break;
            }

            // Entropy: Raise cardinality to length when giver per character
            return (Entropy: Math.Log(perCharCardinality ? guesses : cardinality, 2), Guesses: guesses);
        }//CalculateGuesses
    }//RegexMatcher

    /// <summary>
    /// Regular expression (RegEx) pattern information that consists:
    /// <list type="bullet">
    ///  <item>
    ///   <term><see cref="MatcherRegex"/></term>
    ///   <description>A <see cref="Regex"/> object</description>
    ///  </item>
    ///  <item>
    ///   <term><see cref="Cardinality"/></term>
    ///   <description>Cardinality of this match</description>
    ///  </item>
    ///  <item>
    ///   <term><see cref="PerCharCardinality"/></term>
    ///   <description>Whether cardinality is given for each matched character.</description>
    ///  </item>
    /// </list>
    /// </summary>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Performance", "CA1815:equals および operator equals を値型でオーバーライドします", Justification = "<保留中>")]
    public readonly struct RegexMatcherInfo
    {
        /// <summary>
        /// The <see cref="Regex"/> object used to perform matching
        /// </summary>
        /// <value>The <see cref="Regex"/> object used to perform matching</value>
        public Regex MatcherRegex { get; }

        /// <summary>
        /// The cardinality of this match
        ///
        /// <para>Since this is not able to be calculated from a pattern it must be provided.
        /// It could be given per-match-character or per-match.</para>
        /// </summary>
        /// <value>The cardinality of this match</value>
        public int Cardinality { get; }

        /// <summary>
        /// <c>true</c> if cardinality is given as per-matched-character
        /// </summary>
        /// <value><c>true</c> if cardinality is given as per-matched-character</value>
        public bool PerCharCardinality { get; }

        public RegexMatcherInfo(Regex matchRegex, int cardinality, bool perCharCardinality) =>
            (MatcherRegex, Cardinality, PerCharCardinality) = (matchRegex, cardinality, perCharCardinality);
    }//RegexMatcherInfo

    /// <summary>
    /// A match made with the <see cref="RegexMatcher"/> that contains some additional information
    /// specific to the regular expression match.
    /// </summary>
    public class RegexMatch : Match
    {
        public RegexMatch(int tokenLength, int passwordLength) : base(tokenLength, passwordLength)
        {
        }

        public string RxName { get; set; }

        public System.Text.RegularExpressions.Match RxMatch { get; set; }
    }
}
