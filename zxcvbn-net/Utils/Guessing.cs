using System;
using System.Collections.Generic;
using System.Linq;
using Zxcvbn.Matcher;

namespace Zxcvbn.Utils
{
    public class Guessing
    {
        private const int MinSubmatchGuessesSingleChar = 10;
        private const int MinSubmatchGuessesMultiChar = 50;

        /* Brute-force Guesses */
        public const int BruteforceCardinality = 10;

        /* Sequence Guesses */
        // Lower guesses for obvious starting points
        protected const string StartPoints = "aAzZ019";

        #region Regex Guesses
        protected const int MinYearSpace = 20;
        public static int ReferenceYear { get => DateTime.Today.Year; }

        protected static readonly Dictionary<string, int> CharClassBases = new Dictionary<string, int>
        {
            ["alpha_lower"] = 26,
            ["alpha_upper"] = 26,
            ["alpha"] = 52,
            ["alphanumeric"] = 62,
            ["digits"] = 10,
            ["symbols"] = 33,
        };
        #endregion Regex Guesses

        /// <summary>
        /// Guess estimation - one function per match pattern
        /// </summary>
        /// <param name="match"></param>
        /// <returns></returns>
        public static double EstimateGuess(ref Match match, string password)
        {
            if (match == null) throw new ArgumentNullException(nameof(match));
            if (string.IsNullOrEmpty(password)) throw new ArgumentNullException(nameof(password));

            // A match's guess estimate doesn't change. cache it.
            if (!double.IsNaN(match.Guesses)) return match.Guesses;

            int minGuesses = 1;
            if (match.Token.Length < password.Length)
            {
                minGuesses = match.Token.Length == 1 ? MinSubmatchGuessesSingleChar : MinSubmatchGuessesMultiChar;
            }

            double guesses;
            switch (match.Pattern)
            {
                case Pattern.Bruteforce: guesses = BruteforceGuesses(match); break;
                case Pattern.Dictionary: guesses = DictionaryGuesses((DictionaryMatch)match); break;
                // Spatial match guesses are computed at the matching (see Zxcvbn.Matcher.SpatialGraph.CalculateGuesses)
                case Pattern.Repeat: guesses = RepeatGuesses((RepeatMatch)match); break;
                case Pattern.Sequence: guesses = SequenceGuesses((SequenceMatch)match); break;
                case Pattern.Regex: guesses = RegexGuesses((RegexMatch)match); break;
                case Pattern.Date: guesses = DateGuesses((DateMatch)match); break;
                default: guesses = 0; break;
            }

            match.Guesses = Math.Max(guesses, minGuesses);
            return match.Guesses;
        }//EstimateGuess

        protected static double BruteforceGuesses(in Match match)
        {
            if (match == null) throw new ArgumentNullException(nameof(match));

            double guesses = Math.Pow(BruteforceCardinality, match.Token.Length);
            if (double.IsPositiveInfinity(guesses)) guesses = double.MaxValue;

            // Small detail: make brute-force matches at minimum one guess bigger than smallest allowed
            // sub-match guesses, such that non-brute-force sub-matches over the same [i..j] take precedence.
            double minGuesses = (match.Token.Length == 1 ? MinSubmatchGuessesSingleChar : MinSubmatchGuessesMultiChar) + 1;

            return Math.Max(guesses, minGuesses);
        }//BruteforceGuesses

        #region Dictionary Guesses
        protected static double DictionaryGuesses(in DictionaryMatch dictionaryMatch)
        {
            if (dictionaryMatch == null) throw new ArgumentNullException(nameof(dictionaryMatch));

            double baseGuesses = dictionaryMatch.Rank;
            long uppercaseVariations = UppercaseVariations(dictionaryMatch);
            long l33tVariations = (dictionaryMatch is L33tDictionaryMatch l33tMatch) ? L33tVariations(l33tMatch) : 1;
            int reversedVariations = dictionaryMatch.Reversed ? 2 : 1;

            return baseGuesses * uppercaseVariations * l33tVariations * reversedVariations;
        }//DateGuesses

        private static long UppercaseVariations(in DictionaryMatch match)
        {
            string word = match.Token;
            if (word == word.ToLowerInvariant()) return 1;

            // A capitalized word is the most common capitalization scheme,
            // so it only doubles the search space (uncapitalized + capitalized).
            // All caps and end-capitalized are common enough too, underestimate as 2x factor to be safe.
            if (new[] { word.FirstOrDefault(), word.LastOrDefault() }.Any(c => c >= 'A' && c <= 'Z')
                || word == word.ToUpperInvariant()) return 2;

            // Otherwise, calculate the number of ways to capitalize on whole letters
            // with the number of uppercase letters or less.
            // If there's more uppercase than lower (e.g. PASSwORD),
            // calculate the number of ways to lowercase whole letters with the number of lowercase letters or less.
            int uppers = word.Count(c => c >= 'A' && c <= 'Z');
            int lowers = word.Count(c => c >= 'a' && c <= 'z');

            return Enumerable.Range(1, Math.Min(uppers, lowers)).Sum(i => PasswordScoring.Binomial(uppers + lowers, i));
        }//UppercaseVariations

        private static long L33tVariations(in L33tDictionaryMatch match)
        {
            if (!match.L33t) return 1;

            long variations = 1;
            foreach (var subRef in match.Subs)
            {
                char subbed = subRef.Key;
                char unsubbed = subRef.Value;

                int subbedChars = 0;
                int unsubbedChars = 0;
                // Lowercase match.Token before calculating: capitalization shouldn't affect l33t calculate
                foreach (char chr in match.Token.ToLowerInvariant())
                {
                    if (chr == subbed) subbedChars++;
                    if (chr == unsubbed) unsubbedChars++;
                }

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
                    variations *= Enumerable.Range(1, Math.Min(unsubbedChars, subbedChars))
                                            .Sum(i => PasswordScoring.Binomial(unsubbedChars + subbedChars, i));
                }
            }

            return variations;
        }//L33tVariations
        #endregion Dictionary Guesses

        protected static double RepeatGuesses(in RepeatMatch repeatMatch)
        {
            if (repeatMatch == null) throw new ArgumentNullException(nameof(repeatMatch));
            return repeatMatch.BaseGuesses * repeatMatch.RepeatCount;
        }//RepeatGuesses

        protected static double SequenceGuesses(in SequenceMatch sequenceMatch)
        {
            if (sequenceMatch == null) throw new ArgumentNullException(nameof(sequenceMatch));

            double baseGuesses;
            char firstChar = sequenceMatch.Token[0];
            if (StartPoints.IndexOf(firstChar) != -1) baseGuesses = 4;
            else if (char.IsDigit(firstChar)) baseGuesses = 10;
            // Could give a higher base for uppercase,
            // assigning 26 to both upper and lower sequences is more conservative.
            else baseGuesses = 26;

            // Need to try a descending sequence in addition to every ascending sequence -> 2x guesses
            if (!sequenceMatch.Ascending) baseGuesses *= 2;

            return baseGuesses * sequenceMatch.Token.Length;
        }//SequenceGuesses

        protected static double RegexGuesses(in RegexMatch regexMatch)
        {
            if (regexMatch == null) throw new ArgumentNullException(nameof(regexMatch));

            switch (regexMatch.RxName)
            {
                case "recent_year":
                    // Conservative estimate of year space: number years from ReferenceYear.
                    // if year is close to ReferenceYear, estimate a year space of MinYearSpace.
                    int yearSpace = Math.Abs(regexMatch.RxMatch.Value.ToInt() - ReferenceYear);
                    return Math.Max(yearSpace, MinYearSpace);
                default: return Math.Pow(regexMatch.Cardinality, regexMatch.Token.Length);
            }
        }//RegexGuesses

        protected static double DateGuesses(in DateMatch dateMatch)
        {
            if (dateMatch == null) throw new ArgumentNullException(nameof(dateMatch));

            // Base guesses: (year distance from ReferenceYear) * num_days * num_years
            double yearSpace = Math.Max(Math.Abs(dateMatch.Year - ReferenceYear), MinYearSpace);
            double guesses = yearSpace * 365;

            // Add factor of 4 for separator selection (one of ~4 choices)
            return !string.IsNullOrEmpty(dateMatch.Separator) ? guesses * 4 : guesses;
        }//DateGuesses
    }
}
