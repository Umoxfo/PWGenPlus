using System;
using System.Collections.Generic;
using System.Text;

namespace Zxcvbn.Guess
{
    public abstract class BaseGuess : IGuess
    {
        public const int BruteforceCardinality = 10;
        public const int MinSubmatchGuessesSingleChar = 10;
        public const int MinSubmatchGuessesMultiChar = 50;
        public const int MinYearSpace = 20;
        public const int ReferenceYear = 2000;

        public abstract double Guess(Match match);

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
            for (int d = 1; d <= k; ++d)
            {
                r *= n;
                r /= d;
                n -= 1;
            }

            return r;
        }
    }
}
