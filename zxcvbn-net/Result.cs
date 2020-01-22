using System;
using System.Collections.Generic;

using Umoxfo.Zxcvbn.Report;

namespace Umoxfo.Zxcvbn
{
    /// <summary>
    /// The results of zxcvbn's password analysis
    /// </summary>
    public class Result
    {
        /// <summary>
        /// The password that was used to generate these results.
        /// </summary>
        /// <value>The password that was used to generate these results.</value>
        public string Password { get; protected internal set; }

        /// <summary>
        /// A calculated estimate of how many bits of entropy the password covers.
        /// </summary>
        /// <value>A calculated estimate of how many bits of entropy the password covers.</value>
        public double Entropy { get; protected internal set; }

        /// <summary>
        /// Estimated guesses needed to crack password
        /// </summary>
        /// <value>Estimated guesses needed to crack password</value>
        public double Guesses { get; protected internal set; }

        /// <summary>
        /// Order of <see cref="Guesses"/>
        /// </summary>
        /// <value>Order of <see cref="Guesses"/></value>
        public double GuessesLog10 => Math.Log10(Guesses);

        /// <summary>
        /// The list of matches that were used to create the entropy calculation.
        /// </summary>
        /// <value>The list of matches that were used to create the entropy calculation.</value>
        public IEnumerable<Match> EntropySequence { get; protected internal set; }

        /// <summary>
        /// The list of matches that were used to create the guesses calculation.
        /// </summary>
        /// <value>The list of matches that were used to create the guesses calculation.</value>
        public IEnumerable<Match> GuessesSequence { get; protected internal set; }

        /// <summary>
        /// The list of matches that were used to create the entropy calculation.
        /// </summary>
        /// <value>The list of matches that were used to create the entropy calculation.</value>
        public IEnumerable<Match> MatchSequence => EntropySequence;

        /// <summary>
        /// The number of milliseconds that zxcvbn took to calculate results for this password.
        /// </summary>
        /// <value>The number of milliseconds that zxcvbn took to calculate results for this password.</value>
        public long CalcTime { get; protected internal set; }

        /// <summary>
        /// An estimation of the crack time for this password in seconds.
        /// </summary>
        /// <value>An estimation of the crack time for this password in seconds.</value>
        public CrackTimeSeconds CrackTimeSeconds { get; protected internal set; }

        /// <summary>
        /// A friendly string for the crack time (like "centuries", "instant", "7 minutes", "14 hours" etc.).
        /// </summary>
        /// <value>A friendly string for the crack time (like "centuries", "instant", "7 minutes", "14 hours" etc.).</value>
        public CrackTimeDisplay CrackTimeDisplay { get; protected internal set; }

        /// <summary>
        /// A score from 0 to 6 (inclusive), with 0 being least secure
        /// and 6 being most secure calculated from <see cref="Guesses">guesses</see>
        /// </summary>
        /// <value>[0,1,2,3,4,5,6] if crack time is less than
        /// [10^3, 10^6, 10^8, 10^10, 10^11, 10^12, Infinity] seconds.// </value>
        public int Score { get; protected internal set; }

        /// <summary>
        /// Verbal feedback to help choose better passwords. set when score <= 2.
        /// </summary>
        /// <value>Verbal feedback to help choose better passwords. set when score <= 2.</value>
        public Feedback Feedback { get; protected internal set; }
    }

    public static class Pattern
    {
        public const string Bruteforce = "bruteforce";
        public const string Dictionary = "dictionary";
        public const string Spatial = "spatial";
        public const string Repeat = "repeat";
        public const string Sequence = "sequence";
        public const string Regex = "regex";
        public const string Date = "date";
    }

    /// <summary>
    /// <para>A single match that one of the pattern matchers has made against the password being tested.</para>
    ///
    /// <para>Some pattern matchers implement subclasses of match that can provide more information on their specific results.</para>
    ///
    /// <para>Matches must all have the <see cref="Pattern"/>, <see cref="i"/>, <see cref="j"/>, <see cref="Token"/>, and
    /// <see cref="Entropy"/> fields (i.e. all but the <see cref="Cardinality"/> field, which is optional)
    /// set before being returned from the matcher in which they are created.</para>
    /// </summary>
    public class Match : IComparable<Match>
    {
        private readonly int minGuesses;
        private double guesses;

        /// <summary>
        /// The name of the pattern matcher used to generate this match
        /// </summary>
        /// <value>The name of the pattern matcher used to generate this match</value>
        public string Pattern { get; set; }

        /// <summary>
        /// The start index in the password string of the matched token.
        /// </summary>
        /// <value>The start index in the password string of the matched token</value>
        public int i { get; set; }

        /// <summary>
        /// The end index in the password string of the matched token.
        /// </summary>
        /// <value>The end index in the password string of the matched token</value>
        public int j { get; set; }

        /// <summary>
        /// The portion of the password that was matched
        /// </summary>
        /// <value>The portion of the password that was matched</value>
        public string Token { get; set; }

        // The following are more internal measures, but may be useful to consumers

        /// <summary>
        /// Some pattern matchers can associate the cardinality of the set of possible matches that
        /// the entropy calculation is derived from. Not all matchers provide a value for cardinality.
        /// </summary>
        /// <value>The cardinality of the set of possible matches that the entropy calculation is derived from</value>
        public int Cardinality { get; set; }

        /// <summary>
        /// The entropy that this portion of the password covers using the current pattern matching technique
        /// </summary>
        /// <value>The entropy that this portion of the password covers using the current pattern matching technique</value>
        public virtual double Entropy { get; set; }

        public double Guesses { get => guesses; set => guesses = Math.Max(value, minGuesses); }

        public double GuessesLog10 => Math.Log10(Guesses);

        public Match(int tokenLength, int passwordLength)
        {
            minGuesses = (tokenLength < passwordLength)
                ? tokenLength == 1 ? Scoring.MinSubmatchGuessesSingleChar : Scoring.MinSubmatchGuessesMultiChar
                : 1;

            guesses = Math.Max(0, minGuesses);
        }

        public int CompareTo(Match other)
        {
            if (other == null) return -1;

            // Sort on i primary, j secondary
            int i = this.i - other.i;
            return (i != 0) ? i : (j - other.j);
        }

        public override bool Equals(object obj) => obj is Match match
            && Pattern == match.Pattern
            && i == match.i
            && j == match.j
            && Token == match.Token
            && Cardinality == match.Cardinality
            && Entropy == match.Entropy
            && Guesses == match.Guesses;

        public override int GetHashCode() => HashCode.Combine(Pattern, i, j, Token, Cardinality, Entropy, Guesses);

        public static bool operator ==(Match left, Match right) => (left is null) ? (right is null) : left.Equals(right);

        public static bool operator !=(Match left, Match right) => !(left == right);

        public static bool operator <(Match left, Match right) => (left is null) ? (right is object) : left.CompareTo(right) < 0;

        public static bool operator <=(Match left, Match right) => left is null || left.CompareTo(right) <= 0;

        public static bool operator >(Match left, Match right) => left is object && left.CompareTo(right) > 0;

        public static bool operator >=(Match left, Match right) => (left is null) ? (right is null) : left.CompareTo(right) >= 0;
    }
}
