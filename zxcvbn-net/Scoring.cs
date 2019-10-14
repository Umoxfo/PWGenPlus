using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Zxcvbn.Guess;

namespace Zxcvbn
{
    public static class Scoring
    {
        public const int MinGuessesBeforeGrowingSequence = 10000;

        public static readonly DateTime ReferenceDate = DateTime.Today;

        public static readonly int ReferenceYear = DateTime.Today.Year;

        public static Strength MostGuessableMatchSequence(string password, IEnumerable<Match> matches) =>
            MostGuessableMatchSequence(password, matches, false);

        /*
         * Takes a sequence of overlapping matches, returns the non-overlapping sequence with minimum guesses.
         * The following is a O(l_max * (n + m)) dynamic programming algorithm for
         * a length-n password with m candidate matches. l_max is the maximum optimal
         * sequence length spanning each prefix of the password. In practice it rarely exceeds 5 and
         * the search terminates rapidly.
         *
         * The optimal "minimum guesses" sequence is here defined to be the sequence that
         * minimizes the following function:
         *
         *    g = l! * Product(m.guesses for m in sequence) + D^(l - 1)
         *
         * where l is the length of the sequence.
         *
         * The factorial term is the number of ways to order l patterns.
         *
         * The D^(l-1) term is another length penalty, roughly capturing the idea that
         * an attacker will try lower-length sequences first before trying length-l sequences.
         *
         * For example, consider a sequence that is date-repeat-dictionary.
         *  - An attacker would need to try other date-repeat-dictionary combinations,
         *    hence the product term.
         *  - An attacker would need to try repeat-date-dictionary, dictionary-repeat-date,
         *    ..., hence the factorial term.
         *  - An attacker would also likely try length-1 (dictionary) and length-2 (dictionary-date)
         *    sequences before length-3. assuming at minimum D guesses per pattern type,
         *    D^(l-1) approximates Sum(D^i for i in [1..l-1]
         */
        private static Strength MostGuessableMatchSequence(string password, IEnumerable<Match> matches, bool excludeAdditive)
        {
            if (string.IsNullOrEmpty(password)) throw new ArgumentNullException(nameof(password));

            int n = password.Length;
            List<List<Match>> matchesByJ = new List<List<Match>> { new List<Match>() };

            foreach (var matchGroup in matches.GroupBy(match => match.j))
            {
                matchesByJ.Insert(matchGroup.Key, matchGroup.OrderBy(m => m.i).ToList());
            }

            Optimal optimal = new Optimal(n);
            for (int k = 0; k < n; k++)
            {
                foreach (Match m  in matchesByJ[k])
                {
                    if (m.i > 0)
                    {
                        foreach (int entry in optimal.m[m.i - 1].Keys)
                        {
                            Update(password, m, entry + 1, optimal, excludeAdditive);
                        }
                    }
                    else
                    {
                        Update(password, m, 1, optimal, excludeAdditive);
                    }
                }

                BruteforceUpdate(password, k, optimal, excludeAdditive);
            }

            List<Match> optimalMatchSequence = Unwind(n, optimal);
            int optimalL = optimalMatchSequence.Count;
            double guesses = password.Length== 0 ? 1 : optimal.g[n - 1][optimalL];

            Strength strength = new Strength
            {
                Password = password,
                Guesses = guesses,
                GuessesLog10 = Math.Log10(guesses),
                Sequence = optimalMatchSequence
            };

            return strength;
        }

        private static void Update(string password, Match m, int l, Optimal optimal, bool excludeAdditive)
        {
            int k = m.j;

            double pi = new EstimateGuess(password).Guess(m);
            if (l > 1) pi *= optimal.pi[m.i - 1][l - 1];
            if (double.IsInfinity(pi)) pi = double.MaxValue;

            double g = Factorial(l) * pi;
            if (double.IsInfinity(g)) g = double.MaxValue;

            if (!excludeAdditive)
            {
                g += Math.Pow(MinGuessesBeforeGrowingSequence, l - 1);
                if (double.IsInfinity(g)) g = double.MaxValue;
            }

            foreach (var competing in optimal.g[k])
            {
                if (competing.Key > l) continue;
                if (competing.Value <= g) return;
            }

            optimal.g[k].Add(l, g);
            optimal.m[k].Add(l, m);
            optimal.pi[k].Add(l, pi);
        }

        private static int Factorial(int n)
        {
            if (n < 2) return 1;

            int f = 1;
            for (int i = 2; i <= n; i++) f *= i;

            return f;
        }

        private static List<Match> Unwind(int n, Optimal optimal)
        {
            List<Match> optimalMatchSequence = new List<Match>();
            int k = n - 1;
            if (0 <= k)
            {
                int l = int.MinValue;
                double g = double.PositiveInfinity;

                foreach (var candidate in optimal.g[k])
                {
                    if (candidate.Value < g)
                    {
                        l = candidate.Key;
                        g = candidate.Value;
                    }
                }

                while (k >= 0)
                {
                    Match m = optimal.m[k][l];
                    optimalMatchSequence.Insert(0, m);
                    k = m.i - 1;
                    l--;
                }
            }

            return optimalMatchSequence;
        }

        private static void BruteforceUpdate(string password, int k, Optimal optimal, bool excludeAdditive)
        {
            Match m = MakeBruteforceMatch(password, 0, k);
            Update(password, m, 1, optimal, excludeAdditive);
            for (int i = 1; i <= k; i++)
            {
                m = MakeBruteforceMatch(password, i, k);
                foreach (var entry in optimal.m[i - 1])
                {
                    int l = entry.Key;
                    Match last_m = entry.Value;

                    if (last_m.Pattern == Pattern.Bruteforce) continue;
                    else Update(password, m, l + 1, optimal, excludeAdditive);
                }
            }
        }

        private static Match MakeBruteforceMatch(string password, int i, int j)
        {
            return new Match
            {
                Pattern = Pattern.Bruteforce,
                i = i,
                j = j,
                Token = password.Substring(i, j - i +  1)
            };
        }

        private class Optimal
        {
            public readonly List<Dictionary<int, Match>> m = new List<Dictionary<int, Match>>();

            public readonly List<Dictionary<int, double>> pi = new List<Dictionary<int, double>>();

            public readonly List<Dictionary<int, double>> g = new List<Dictionary<int, double>>();

            public Optimal(int n)
            {
                for (int i = 0; i < n; i++)
                {
                    m.Add(new Dictionary<int, Match>());
                    pi.Add(new Dictionary<int, double>());
                    g.Add(new Dictionary<int, double>());
                }
            }
        }
    }
}
