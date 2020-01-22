using System;
using System.Collections.Generic;

using Umoxfo.Zxcvbn.Matcher;
using Umoxfo.Zxcvbn.Report;

namespace Umoxfo.Zxcvbn
{
    /// <summary>
    /// <para>Zxcvbn is used to estimate the strength of passwords. </para>
    ///
    /// <para>This implementation is a port of the Zxcvbn JavaScript library by Dan Wheeler:
    /// https://github.com/lowe/zxcvbn </para>
    ///
    /// <para>To quickly evaluate a password, use the <see cref="MeasurePassword"/> static function.</para>
    ///
    /// <para>To evaluate a number of passwords, create an instance of this object and repeatedly call
    /// the <see cref="EvaluatePassword"/> function.
    /// Reusing the Zxcvbn instance will ensure that pattern matchers will only be created once
    /// rather than being recreated for each password evaluated.</para>
    /// </summary>
    public class Zxcvbn
    {
        private readonly Matching matcherFactory;
        private readonly Translation translation;

        /// <summary>
        /// Create a new instance of Zxcvbn that uses the default matchers and user inputs list.
        /// </summary>
        public Zxcvbn(IEnumerable<string> userInputs = null, Translation translation = Translation.English)
            : this(new DefaultMatcherFactory(userInputs), translation)
        {
        }

        /// <summary>
        /// Create an instance of Zxcvbn that will use the given matcher factory to create matchers to use
        /// to find password weakness.
        /// </summary>
        /// <param name="matcherFactory">The factory used to create the pattern matchers used</param>
        /// <param name="translation">The language in which the strings are returned</param>
        public Zxcvbn(Matching matcherFactory, Translation translation = Translation.English)
        {
            this.matcherFactory = matcherFactory;
            this.translation = translation;
        }

        /// <summary>
        /// <para>A static function to match a password against the default matchers without having to create
        /// an instance of Zxcvbn yourself, with supplied user data. </para>
        ///
        /// <para>Supplied user data will be treated as another kind of dictionary matching.</para>
        /// </summary>
        /// <param name="password">A password string to test</param>
        /// <param name="userInputs">Optionally, the user inputs list</param>
        /// <returns>The results of the password evaluation</returns>
        public static Result MeasurePassword(string password, IEnumerable<string> userInputs = null) =>
            new Zxcvbn(new DefaultMatcherFactory(userInputs)).EvaluatePassword(password);

        /// <summary>
        /// <para>Perform the password matching on the given password and user inputs,
        /// returning the result structure with information on the lowest entropy match found.</para>
        ///
        /// <para>User data will be treated as another kind of dictionary matching,
        /// but can be different for each password being evaluated.</para>
        /// </summary>
        /// <param name="password">A password string to test</param>
        /// <param name="userInputs">Optionally, an enumerable of user data</param>
        /// <returns>Result for the lowest entropy match</returns>
        public Result EvaluatePassword(string password, IEnumerable<string> userInputs = null)
        {
            //userInputs = userInputs ?? Array.Empty<string>();
            System.Diagnostics.Stopwatch timer = System.Diagnostics.Stopwatch.StartNew();

            if (userInputs != null) matcherFactory.SetUserInputDictionary(userInputs);

            var matches = matcherFactory.Omnimatch(password);

            Result result = Scoring.FindBestMatchSequences(password, matches);

            timer.Stop();
            result.CalcTime = timer.ElapsedMilliseconds;

            (result.CrackTimeSeconds, result.CrackTimeDisplay) = TimeEstimates.EstimateAttackTimes(result.Guesses, translation);
            result.Score = PasswordScoring.GuessesToScore(result.Guesses);

            result.Feedback = PasswordFeedback.GetFeedback(result.Score, result.GuessesSequence, translation);

            return result;
        }//EvaluatePassword

        /// <summary>
        /// Returns a password score for password matching.
        /// </summary>
        /// <param name="password">A password string to test</param>
        /// <param name="userInputs">Optionally, an enumerable of user data</param>
        /// <returns>Password score of 0-6, 0 is minimum</returns>
        public int CalculatePasswordScore(string password, IEnumerable<string> userInputs = null)
        {
            if (userInputs != null) matcherFactory.SetUserInputDictionary(userInputs);

            Result result = Scoring.FindBestMatchSequences(password, matcherFactory.Omnimatch(password));

            return PasswordScoring.GuessesToScore(result.Guesses);
        }//CalculatePasswordScore

        /// <summary>
        /// Returns a password score for brute-force matching.
        /// </summary>
        /// <param name="password">A password string to test</param>
        /// <returns>Password score of 0-6, 0 is minimum</returns>
        public static int CalculateBruteforcePasswordScore(string password)
        {
            if (string.IsNullOrEmpty(password)) throw new ArgumentNullException(nameof(password));

            double guesses = Math.Pow(PasswordScoring.PasswordCardinality(password), password.Length);
            if (double.IsPositiveInfinity(guesses)) guesses = double.MaxValue;

            // Small detail: make brute-force matches at minimum one guess bigger than smallest allowed
            // sub-match guesses, such that non-brute-force sub-matches over the same [i..j] take precedence.
            double minGuesses = (password.Length == 1 ? Scoring.MinSubmatchGuessesSingleChar : Scoring.MinSubmatchGuessesMultiChar) + 1;

            return PasswordScoring.GuessesToScore(Math.Max(guesses, minGuesses));
        }//CalculateBruteforcePasswordScore
    }
}
