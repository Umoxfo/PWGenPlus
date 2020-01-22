using System;
using System.Collections.Generic;
using System.Linq;

namespace Umoxfo.Zxcvbn.Matcher
{
    /// <summary>
    /// This matcher detects lexicographical sequences (and in reverse) e.g. abcd, 4567, PONML etc.
    /// </summary>
    public class SequenceMatcher : IMatcher
    {
        private const int MaxDelta = 5;

        // Lower guesses for obvious starting points
        private const string StartPoints = "aAzZ019";

        /// <summary>
        /// Find matching sequences in <paramref name="password"/>
        /// </summary>
        /// <param name="password">The password to match</param>
        /// <returns>Enumerable of sequence matches</returns>
        /// <seealso cref="SequenceMatch"/>
        public IEnumerable<Match> MatchPassword(string password)
        {
            // Identifies sequences by looking for repeated differences in Unicode code-point.
            // this allows skipping, such as 9753, and also matches some extended Unicode sequences
            // such as Greek and Cyrillic alphabets.
            //
            // for example, consider the input 'abcdb975zy'
            //
            // password: a   b   c   d   b    9   7   5   z   y
            // index:    0   1   2   3   4    5   6   7   8   9
            // delta:      1   1   1  -2  -41  -2  -2  69   1
            //
            // expected result:
            // [(i, j, delta), ...] = [(0, 3, 1), (5, 7, -2), (8, 9, 1)]

            if (string.IsNullOrEmpty(password)) throw new ArgumentNullException(nameof(password));
            if (password.Length <= 1) yield break;

            int i = 0;
            int lastDelta = password[1] - password[0];
            for (int k = 2; k < password.Length; k++)
            {
                int j = k - 1;
                int delta = password[k] - password[j];

                if (k != password.Length && delta == lastDelta) continue;

                Match match = Update(password, i, j, lastDelta);
                if (match != null) yield return match;

                i = j;
                lastDelta = delta;
            }//for
        }

        private static SequenceMatch Update(string password, int i, int j, int delta)
        {
            if ((j - i) > 1 || Math.Abs(delta) == 1)
            {
                if (0 < Math.Abs(delta) && Math.Abs(delta) <= MaxDelta)
                {
                    string token = password.Substring(i, j - i + 1);

                    string sequenceName;
                    int sequenceSpace;
                    if (token.All(c => c >= 'a' && c <= 'z'))
                    {
                        sequenceName = "lower";
                        sequenceSpace = 26;
                    }
                    else if (token.All(c => c >= 'A' && c <= 'Z'))
                    {
                        sequenceName = "upper";
                        sequenceSpace = 26;
                    }
                    else if (token.All(c => c >= '0' && c <= '9'))
                    {
                        sequenceName = "digits";
                        sequenceSpace = 10;
                    }
                    else if (token.All(c => (c >= ' ' && c <= '/') || (c >= ':' && c <= '@') || (c >= '[' && c <= '`') || (c >= '{' && c <= '~')))
                    {
                        sequenceName = "symbols";
                        sequenceSpace = 33;
                    }
                    else
                    {
                        sequenceName = "unicode";
                        // Unicode printable characters other than ASCII characters (U+0000 to U+007F):
                        //  Line and paragraph separate characters (U+2028 and U+2029)
                        //  Control code characters in the range U+0080 through U+009F
                        //  Format character belonging to "Cf" (other, format) in Unicode (161)
                        sequenceSpace = PasswordScoring.UnicodePrintableCharacters;
                    }//if - else if - else

                    bool ascending = delta > 0;

                    return new SequenceMatch(token.Length, password.Length)
                    {
                        Pattern = Pattern.Sequence,
                        i = i,
                        j = j,
                        Token = token,
                        SequenceName = sequenceName,
                        SequenceSize = sequenceSpace,
                        Ascending = ascending,

                        Entropy = CalculateEntropy(token, ascending),
                        Guesses = CalculateGuesses(token, ascending)
                    };
                }//if
            }//if

            return null;
        }//Update

        private static double CalculateGuesses(string match, bool ascending)
        {
            char firstChar = match[0];

            double baseGuesses;
            if (StartPoints.IndexOf(firstChar) != -1) baseGuesses = 4;
            else if (char.IsDigit(firstChar)) baseGuesses = 10;
            // Could give a higher base for uppercase,
            // assigning 26 to both upper and lower sequences is more conservative.
            else baseGuesses = 26;

            // Need to try a descending sequence in addition to every ascending sequence -> 2x guesses
            if (!ascending) baseGuesses *= 2;

            return baseGuesses * match.Length;
        }//CalculateGuesses

        private static double CalculateEntropy(string match, bool ascending)
        {
            char firstChar = match[0];

            // XXX: This entropy calculation is hard coded, ideally this would (somehow) be derived from the sequences above
            double baseEntropy;
            if (firstChar == 'a' || firstChar == '1') baseEntropy = 1;
            else if (char.IsDigit(firstChar)) baseEntropy = Math.Log(10, 2); // Numbers
            else if (char.IsLower(firstChar)) baseEntropy = Math.Log(26, 2); // Lowercase
            else baseEntropy = Math.Log(26, 1) + 1; // + 1 for uppercase

            if (!ascending) baseEntropy += 1; // Descending instead of ascending give + 1 bit of entropy

            return baseEntropy + Math.Log(match.Length, 2);
        }
    }

    /// <summary>
    /// A match made with the <see cref="SequenceMatcher"/>
    /// that contains some additional information specific to the sequence match.
    /// </summary>
    public class SequenceMatch : Match
    {
        public SequenceMatch(int tokenLength, int passwordLength) : base(tokenLength, passwordLength)
        {
        }

        /// <summary>
        /// The name of the sequence that the match was found in (e.g. 'lower', 'upper', 'digits')
        /// </summary>
        public string SequenceName { get; set; }

        /// <summary>
        /// The size of the sequence the match was found in (e.g. 26 for lowercase letters)
        /// </summary>
        public int SequenceSize { get; set; }

        /// <summary>
        /// Whether the match was found in ascending order (cdefg) or not (zyxw)
        /// </summary>
        public bool Ascending { get; set; }
    }
}
