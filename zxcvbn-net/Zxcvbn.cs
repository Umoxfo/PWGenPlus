using System;
using System.Collections.Generic;
using System.Linq;

using Zxcvbn.Matcher;
using Zxcvbn.Report;

namespace Zxcvbn
{
    /// <summary>
    /// <para>Zxcvbn is used to estimate the strength of passwords. </para>
    ///
    /// <para>This implementation is a port of the Zxcvbn JavaScript library by Dan Wheeler:
    /// https://github.com/lowe/zxcvbn </para>
    ///
    /// <para>To quickly evaluate a password, use the <see cref="MatchPassword"/> static function.</para>
    ///
    /// <para>To evaluate a number of passwords, create an instance of this object and repeatedly call
    /// the <see cref="EvaluatePassword"/> function.
    /// Reusing the Zxcvbn instance will ensure that pattern matchers will only be created once
    /// rather than being recreated for each password evaluated.</para>
    /// </summary>
    public class Zxcvbn
    {
        private const string BruteforcePattern = "bruteforce";

        private readonly IMatcherFactory matcherFactory;
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
        public Zxcvbn(IMatcherFactory matcherFactory, Translation translation = Translation.English)
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
        /// <param name="password">the password to test</param>
        /// <param name="userInputs">optionally, the user inputs list</param>
        /// <returns>The results of the password evaluation</returns>
        public static Result MeasurePassword(string password, IEnumerable<string> userInputs = null) =>
            new Zxcvbn(new DefaultMatcherFactory()).EvaluatePassword(password, userInputs);

        /// <summary>
        /// <para>Perform the password matching on the given password and user inputs,
        /// returning the result structure with information on the lowest entropy match found.</para>
        ///
        /// <para>User data will be treated as another kind of dictionary matching,
        /// but can be different for each password being evaluated.</para>
        /// </summary>
        /// <param name="password">Password</param>
        /// <param name="userInputs">Optionally, an enumerable of user data</param>
        /// <returns>Result for lowest entropy match</returns>
        public Result EvaluatePassword(string password, IEnumerable<string> userInputs = null)
        {
            userInputs = userInputs ?? Array.Empty<string>();

            // Reset the user inputs matcher on a per-request basis to keep things stateless
            IEnumerable<string> sanitizedInputs = userInputs.Select(sanitizedInput => sanitizedInput.ToLowerInvariant());
            matcherFactory.CreateMatchers(sanitizedInputs);

            IEnumerable<Match> matches = new List<Match>();
            foreach (IMatcher matcher in matcherFactory.CreateMatchers(userInputs))
            {
                matches = matches.Union(matcher.MatchPassword(password));
            }

            System.Diagnostics.Stopwatch timer = System.Diagnostics.Stopwatch.StartNew();

            Result result = Scoring.FindBestMatchSequences(password, matches);

            timer.Stop();
            result.CalcTime = timer.ElapsedMilliseconds;

            (result.CrackTime, result.CrackTimeDisplay) = TimeEstimates.EstimateAttackTimes(result.Guesses, translation);
            result.Score = PasswordScoring.GuessesToScore(result.Guesses);

            result.Feedback = PasswordFeedback.GetFeedback(result.Score, result.GuessesSequence, translation);

            return result;
        }
    }
}
