using System;
using System.Collections.Generic;

// TODO: These should probably be immutable
namespace Zxcvbn
{
    /// <summary>
    /// SWarning associated with the password analysis
    /// </summary>
    public enum Warning
    {
        /// <summary>
        /// Empty string
        /// </summary>
        Default,

        /// <summary>
        /// Straight rows of keys are easy to guess
        /// </summary>
        StraightRow,

        /// <summary>
        /// Short keyboard patterns are easy to guess
        /// </summary>
        ShortKeyboardPatterns,

        /// <summary>
        /// Repeats like "aaa" are easy to guess
        /// </summary>
        RepeatsLikeAaaEasy,

        /// <summary>
        /// Repeats like "abcabcabc" are only slightly harder to guess than "abc"
        /// </summary>
        RepeatsLikeAbcSlighterHarder,

        /// <summary>
        /// Sequences like abc or 6543 are easy to guess
        /// </summary>
        SequenceAbcEasy,

        /// <summary>
        /// Recent years are easy to guess
        /// </summary>
        RecentYearsEasy,

        /// <summary>
        ///  Dates are often easy to guess
        /// </summary>
        DatesEasy,

        /// <summary>
        ///  This is a top-10 common password
        /// </summary>
        Top10Passwords,

        /// <summary>
        /// This is a top-100 common password
        /// </summary>
        Top100Passwords,

        /// <summary>
        /// This is a very common password
        /// </summary>
        CommonPasswords,

        /// <summary>
        /// This is similar to a commonly used password
        /// </summary>
        SimilarCommonPasswords,

        /// <summary>
        /// A word by itself is easy to guess
        /// </summary>
        WordEasy,

        /// <summary>
        /// Names and surnames by themselves are easy to guess
        /// </summary>
        NameSurnamesEasy,

        /// <summary>
        /// Common names and surnames are easy to guess
        /// </summary>
        CommonNameSurnamesEasy,

        /// <summary>
        ///  Empty String
        /// </summary>
        Empty,
    }

    /// <summary>
    /// Suggestion on how to improve the password base on zxcvbn's password analysis
    /// </summary>
    public enum Suggestion
    {
        /// <summary>
        ///  Use a few words, avoid common phrases
        ///  No need for symbols, digits, or uppercase letters
        /// </summary>
        Default,

        /// <summary>
        ///  Add another word or two. Uncommon words are better.
        /// </summary>
        AddAnotherWordOrTwo,

        /// <summary>
        ///  Use a longer keyboard pattern with more turns
        /// </summary>
        UseLongerKeyboardPattern,

        /// <summary>
        ///  Avoid repeated words and characters
        /// </summary>
        AvoidRepeatedWordsAndChars,

        /// <summary>
        ///  Avoid sequences
        /// </summary>
        AvoidSequences,

        /// <summary>
        ///  Avoid recent years
        ///  Avoid years that are associated with you
        /// </summary>
        AvoidYearsAssociatedYou,

        /// <summary>
        ///  Avoid dates and years that are associated with you
        /// </summary>
        AvoidDatesYearsAssociatedYou,

        /// <summary>
        ///  Capitalization doesn't help very much
        /// </summary>
        CapsDontHelp,

        /// <summary>
        /// All-uppercase is almost as easy to guess as all-lowercase
        /// </summary>
        AllCapsEasy,

        /// <summary>
        /// Reversed words aren't much harder to guess
        /// </summary>
        ReversedWordEasy,

        /// <summary>
        ///  Predictable substitutions like '@' instead of 'a' don't help very much
        /// </summary>
        PredictableSubstitutionsEasy,

        /// <summary>
        ///  Empty String
        /// </summary>
        Empty,
    }

    /// <summary>
    /// The results of zxcvbn's password analysis
    /// </summary>
    public class Result
    {
        /// <summary>
        /// A calculated estimate of how many bits of entropy the password covers.
        /// </summary>
        public double Entropy { get; internal set; }

        /// <summary>
        /// The number of milliseconds that zxcvbn took to calculate results for this password
        /// </summary>
        public long CalcTime { get; internal set; }

        /// <summary>
        /// An estimation of the crack time for this password in seconds
        /// </summary>
        public double CrackTime { get; internal set; }

        /// <summary>
        /// A friendly string for the crack time (like "centuries", "instant", "7 minutes", "14 hours" etc.)
        /// </summary>
        public string CrackTimeDisplay { get; internal set; }

        /// <summary>
        /// A score from 0 to 6 (inclusive), with 0 being least secure and 6 being most secure calculated from crack time:
        /// [0,1,2,3,4,5,6] if crack time is less than [10^3, 10^6, 10^8, 10^10, 10^11, 10^12, Infinity] seconds.
        /// Useful for implementing a strength meter
        /// </summary>
        public int Score { get; internal set; }

        /// <summary>
        /// The sequence of matches that were used to create the entropy calculation
        /// </summary>
        public IList<Match> MatchSequence { get; internal set; }

        /// <summary>
        /// The password that was used to generate these results
        /// </summary>
        public string Password { get; internal set; }

        /// <summary>
        /// Warning on this password
        /// </summary>
        public Warning Warning { get; internal set; }

        /// <summary>
        /// Suggestion on how to improve the password
        /// Initialized by the Suggestion list
        /// </summary>
        public List<Suggestion> Suggestions { get; internal set; } = new List<Suggestion>();
    }

    public readonly struct Pattern : IEquatable<Pattern>
    {
        public const string Bruteforce = "bruteforce";
        public const string Dictionary = "dictionary";
        public const string Spatial = "spatial";
        public const string Repeat = "repeat";
        public const string Sequence = "sequence";
        public const string Regex = "regex";
        public const string Date = "date";

        public override bool Equals(object obj) => (obj is Pattern pattern) && Equals(pattern);

        public bool Equals(Pattern other) => GetHashCode() == other.GetHashCode();

        public override int GetHashCode()
        {
            int hashCode = 478571960;

            hashCode *= -1521134295 + EqualityComparer<string>.Default.GetHashCode(Bruteforce);
            hashCode *= -1521134295 + EqualityComparer<string>.Default.GetHashCode(Dictionary);
            hashCode *= -1521134295 + EqualityComparer<string>.Default.GetHashCode(Spatial);
            hashCode *= -1521134295 + EqualityComparer<string>.Default.GetHashCode(Repeat);
            hashCode *= -1521134295 + EqualityComparer<string>.Default.GetHashCode(Sequence);
            hashCode *= -1521134295 + EqualityComparer<string>.Default.GetHashCode(Regex);
            hashCode *= -1521134295 + EqualityComparer<string>.Default.GetHashCode(Date);

            return hashCode;
        }

        public static bool operator ==(Pattern left, Pattern right) => left.Equals(right);

        public static bool operator !=(Pattern left, Pattern right) => !(left == right);
    }

    /// <summary>
    /// <para>A single match that one of the pattern matchers has made against the password being tested.</para>
    ///
    /// <para>Some pattern matchers implement subclasses of match that can provide more information on their specific results.</para>
    ///
    /// <para>Matches must all have the <see cref="Match.Pattern"/>, <see cref="Token"/>, <see cref="Entropy"/>, <see cref="i"/> and
    /// <see cref="j"/> fields (i.e. all but the <see cref="Cardinality"/> field, which is optional) set before being returned from the matcher in which they are created.</para>
    /// </summary>
    public class Match : IComparable<Match>
    {
        /// <summary>
        /// The name of the pattern matcher used to generate this match
        /// </summary>
        public string Pattern { get; set; }

        /// <summary>
        /// The start index in the password string of the matched token.
        /// </summary>
        public int i { get; set; } // Start Index

        /// <summary>
        /// The end index in the password string of the matched token.
        /// </summary>
        public int j { get; set; } // End Index

        /// <summary>
        /// The portion of the password that was matched
        /// </summary>
        public string Token { get; set; }

        // The following are more internal measures, but may be useful to consumers

        /// <summary>
        /// Some pattern matchers can associate the cardinality of the set of possible matches that
        /// the entropy calculation is derived from. Not all matchers provide a value for cardinality.
        /// </summary>
        public int Cardinality { get; set; }

        /// <summary>
        /// The entropy that this portion of the password covers using the current pattern matching technique
        /// </summary>
        public double Entropy { get; set; }

        public double Guesses { get; internal set; } = double.NaN;

        public double GuessesLog10 { get; internal set; }

        public int CompareTo(Match other)
        {
            if (other is null) return -1;

            // Sort on i primary, j secondary
            int i = this.i - other.i;
            return (i != 0) ? i : (this.j - other.j);
        }

        public override bool Equals(object obj) => (obj is Match match) && (GetHashCode() == match.GetHashCode());

        public override int GetHashCode()
        {
            int hashCode = 478571960;

            hashCode *= -1521134295 + EqualityComparer<string>.Default.GetHashCode(Pattern);
            hashCode *= -1521134295 + i.GetHashCode();
            hashCode *= -1521134295 + j.GetHashCode();
            hashCode *= -1521134295 + EqualityComparer<string>.Default.GetHashCode(Token);
            hashCode *= -1521134295 + Cardinality.GetHashCode();
            hashCode *= -1521134295 + Entropy.GetHashCode();
            hashCode *= -1521134295 + Guesses.GetHashCode();
            hashCode *= -1521134295 + GuessesLog10.GetHashCode();

            return hashCode;
        }

        public static bool operator ==(Match left, Match right) => (left is null) ? (right is null) : left.Equals(right);

        public static bool operator !=(Match left, Match right) => !(left == right);

        public static bool operator <(Match left, Match right) => (left is null) ? (right is object) : left.CompareTo(right) < 0;

        public static bool operator <=(Match left, Match right) => left is null || left.CompareTo(right) <= 0;

        public static bool operator >(Match left, Match right) => left is object && left.CompareTo(right) > 0;

        public static bool operator >=(Match left, Match right) => (left is null) ? (right is null) : left.CompareTo(right) >= 0;
    }
}
