using System;
using System.Linq;

namespace Umoxfo.Zxcvbn
{
    /// <summary>
    /// Some useful shared functions used for evaluating passwords
    /// </summary>
    internal static class PasswordScoring
    {
        // Named characters in the Unicode standard version 8.0
        private const int UnicodeCharacters = 120_672;

        // Unicode printable characters other than ASCII characters (U+0000 to U+007F):
        //  Line and paragraph separate characters (U+2028 and U+2029)
        //  Control code characters in the range U+0080 through U+009F
        //  Format character belonging to "Cf" (other, format) in Unicode (161)
        internal const int UnicodePrintableCharacters = UnicodeCharacters - (128 + 2 + 32 + 161);

        private readonly struct Score
        {
            private const int DELTA = 7;

            public const double Useless = 1e3 + DELTA;
            public const double VeryWeek = 1e6 + DELTA;
            public const double Week = 1e8 + DELTA;
            public const double Reasonable = 1e10 + DELTA;
            public const double Good = 1e11 + DELTA;
            public const double Strong = 1e12 + DELTA;
            public const double VeryStrong = 1e13 + DELTA;
        }

        /// <summary>
        /// Calculate binomial coefficient (i.e. nCk)
        /// Uses same algorithm as zxcvbn (cf. scoring.coffee), from http://blog.plover.com/math/choose.html
        /// </summary>
        /// <param name="k">k</param>
        /// <param name="n">n</param>
        /// <returns>Binomial coefficient; nCk</returns>
        public static long Binomial(int n, int k)
        {
            if (k > n) return 0;
            if (k == 0) return 1;

            long r = 1;
            for (int d = 1; d <= k; d++)
            {
                r *= n;
                r /= d;
                n -= 1;
            }

            return r;
        }//Binomial

        /// <summary>
        /// Calculate the cardinality of the minimal character sets necessary to brute force the password (roughly)
        /// (e.g. lowercase = 26, numbers = 10, lowercase + numbers = 36)
        /// </summary>
        /// <param name="password">The password to evaluate</param>
        /// <returns>An estimation of the cardinality of characters for this password</returns>
        public static int PasswordCardinality(string password)
        {
            var (lowercase, uppercase, digits, symbols, unicode) = (false, false, false, false, false);

            foreach (char c in password)
            {
                if (c >= 'a' && c <= 'z') lowercase = true;
                else if (c >= 'A' && c <= 'Z') uppercase = true;
                else if (c >= '0' && c <= '9') digits = true;
                else if ((c >= ' ' && c <= '/')
                    || (c >= ':' && c <= '@')
                    || (c >= '[' && c <= '`')
                    || (c >= '{' && c <= '~')) symbols = true;
                else if (!char.IsControl(c)) unicode = true;
            }//foreach

            int cl = 0;
            if (lowercase) cl += 26; // Lowercase
            if (uppercase) cl += 26; // Uppercase
            if (digits) cl += 10; // Digits
            if (symbols) cl += 33; // Symbols
            if (unicode) cl += UnicodePrintableCharacters; // 'Unicode 8.0'

            return cl;
        }//PasswordCardinality

        /// <summary>
        /// Estimate the extra entropy and "guesses" in a token that comes from mixing upper and lowercase letters.
        /// </summary>
        /// <param name="word">The word to calculate uppercase entropy and "guesses" for</param>
        /// <returns>An estimation of the entropy and "guesses" gained from casing in <paramref name="word"/></returns>
        public static (double Entropy, long Guesses) CalculateUppercaseVariations(string word)
        {
            if (word == word.ToLowerInvariant()) return (Entropy: 0, Guesses: 1);

            // A capitalized word is the most common capitalization scheme,
            // so it only doubles the search space (uncapitalized + capitalized).
            // All caps and end-capitalized are common enough too, underestimate as 2x factor to be safe.
            // For entropy, adding only one bit
            if (new[] { word.FirstOrDefault(), word.LastOrDefault() }.Any(c => c >= 'A' && c <= 'Z')
                || word == word.ToUpperInvariant()) return (Entropy: 1, Guesses: 2);

            // Otherwise, calculate the number of ways to capitalize on whole letters
            // with the number of uppercase letters or less.
            // If there's more uppercase than lower (e.g. PASSwORD),
            // calculate the number of ways to lowercase whole letters with the number of lowercase letters or less.
            int uppers = 0;
            int lowers = 0;
            foreach (char c in word)
            {
                if (c >= 'A' && c <= 'Z') uppers++;
                if (c >= 'a' && c <= 'z') lowers++;
            }

            // Calculate the number of ways to capitalize (or inverse if there are fewer lowercase chars)
            long guesses = Enumerable.Range(1, Math.Min(uppers, lowers)).Sum(i => Binomial(uppers + lowers, i));

            // log for entropy
            return (Entropy: Math.Log(guesses + 1, 2), Guesses: guesses);
        }//CalculateUppercaseVariations

        /// <summary>
        /// Returns a score for password strength from the <see cref="Result.Guesses"/>.
        /// Scores are 0..6, 0 being minimum and 6 maximum strength.
        /// </summary>
        /// <param name="guesses">Guesses of password</param>
        /// <returns>Password strength of 0-6, 0 is minimum</returns>
        public static int GuessesToScore(double guesses)
        {
            if (guesses < Score.Useless) return 0;
            else if (guesses < Score.VeryWeek) return 1;
            else if (guesses < Score.Week) return 2;
            else if (guesses < Score.Reasonable) return 3;
            else if (guesses < Score.Good) return 4;
            else if (guesses < Score.Strong) return 5;
            else return 6;
        }//CrackTimeToScore

        /// <summary>
        /// Returns a score for password strength from the entropy.
        /// Scores are 0..6, 0 being minimum and 6 maximum strength.
        /// </summary>
        /// <param name="entropy">Entropy of password</param>
        /// <returns>Password strength of 0-6, 0 is minimum</returns>
        public static int EntropyToScore(double entropy)
        {
            // Calculates a rough estimate of crack time for entropy,
            // see zxcbn scoring.coffee for more information on the model used
            double EntropyToCrackTime()
            {
                const double singleGuess = 0.01;
                const double numAttackers = 100;
                const double secondsPerGuess = singleGuess / numAttackers;

                return 0.5 * Math.Pow(2, entropy) * secondsPerGuess;
            }

            double crackTime = EntropyToCrackTime();

            if (crackTime < Score.Useless) return 0;
            else if (crackTime < Score.VeryWeek) return 1;
            else if (crackTime < Score.Week) return 2;
            else if (crackTime < Score.Reasonable) return 3;
            else if (crackTime < Score.Good) return 4;
            else if (crackTime < Score.Strong) return 5;
            else return 6;
        }//EntropyToScore
    }
}
