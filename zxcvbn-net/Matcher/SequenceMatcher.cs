using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace Zxcvbn.Matcher
{
    /// <summary>
    /// This matcher detects lexicographical sequences (and in reverse) e.g. abcd, 4567, PONML etc.
    /// </summary>
    public class SequenceMatcher : IMatcher
    {
        private const int MaxDelta = 5;

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
            if (password.Length <= 1) yield return new Match();

            int i = 0;
            int lastDelta = password[1] - password[0];
            for (int k = 2; k <= password.Length; k++)
            {
                int delta = 0;
                int j = k - 1;

                if (k != password.Length && (delta = password[k] - password[j]) == lastDelta) continue;

                Match match = Update(password, i, j, lastDelta);
                if (match != null) yield return match;

                i = j;
                lastDelta = delta;
            }
        }

        private static Match Update(string password, int i, int j, int delta)
        {
            if ((j - i) > 1 || Math.Abs(delta) == 1)
            {
                string token;
                if (Math.Abs(delta) > 0 && Math.Abs(delta) <= MaxDelta)
                {
                    token = password.Substring(i, j - i + 1);

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
                        // Maximum possible Unicode size excluding lowercase and uppercase alphabets (52), digits (10), and symbols (33).
                        sequenceSpace = char.MaxValue - 95;
                    }//if - else if - else

                    bool ascending = delta > 0;

                    return new SequenceMatch
                    {
                        Pattern = Pattern.Sequence,
                        i = i,
                        j = j,
                        Token = token,
                        SequenceName = sequenceName,
                        SequenceSize = sequenceSpace,
                        Ascending = ascending,

                        Entropy = CalculateEntropy(token, ascending)
                    };
                }//if
            }//if

            return null;
        }//Update

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
