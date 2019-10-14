using System;
using System.Collections.Generic;
using System.Text;

namespace Zxcvbn.Guess
{
    public class EstimateGuess : BaseGuess
    {
        private readonly string password;

        public EstimateGuess(string password) => this.password = password;

        public override double Guess(Match match)
        {
            if (!double.IsNaN(match.Guesses)) return match.Guesses;

            int minGuesses = 1;
            if (match.Token.Length < password.Length)
            {
                minGuesses = match.Token.Length == 1 ? MinSubmatchGuessesSingleChar : MinSubmatchGuessesMultiChar;
            }

            IGuess guess;
            switch (match.Pattern)
            {
                case Pattern.Bruteforce: guess = new BruteforceGuess(); break;
                case Pattern.Dictionary: guess = new DictionaryGuess(); break;
                case Pattern.Spatial: guess = new SpatialGuess(); break;
                case Pattern.Repeat: guess = new RepeatGuess(); break;
                case Pattern.Sequence: guess = new SequenceGuess(); break;
                case Pattern.Regex: guess = new RegexGuess(); break;
                case Pattern.Date: guess = new DateGuess(); break;
                default: guess = null; break;
            }

            double guesses = guess != null ? guess.Guess(match) : 0;
            match.Guesses = Math.Max(guesses, minGuesses);
            match.GuessesLog10 = Math.Log10(match.Guesses);

            return match.Guesses;
        }
    }
}
