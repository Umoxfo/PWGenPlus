using System;
using System.Collections.Generic;
using System.Linq;

namespace Umoxfo.Zxcvbn.Report
{
    public static class Scoring
    {
        public const int MinSubmatchGuessesSingleChar = 10;
        public const int MinSubmatchGuessesMultiChar = 50;

        public const int MinGuessesBeforeGrowingSequence = 10000;
        public const int BruteforceCardinality = 10;

        public const int MinYearSpace = 20;
        public static DateTime ReferenceDate => DateTime.Today;
        public static int ReferenceYear => DateTime.Today.Year;

        /// <summary>
        /// Returns a new result structure initialized with a minimum entropy of
        /// all passed matches and a non-overlapping sequence with a minimum guess;
        /// adding brute-force matches where there is no smaller entropy found pattern matches.
        /// </summary>
        /// <remarks>
        /// The following is a O(l_max* (n + m)) dynamic programming algorithm for
        /// a length-n password with m candidate matches.l_max is the maximum optimal
        /// sequence length spanning each prefix of the password.In practice it rarely exceeds 5 and
        /// the search terminates rapidly.
        ///
        /// The optimal "minimum guesses" sequence is here defined to be the sequence that
        /// minimizes the following function:
        ///
        ///    g = l! * Product(m.guesses for m in sequence) + D ^ (l - 1)
        ///
        /// where l is the length of the sequence.
        ///
        /// The factorial term is the number of ways to order l patterns.
        ///
        /// The D^(l-1) term is another length penalty, roughly capturing the idea that
        /// an attacker will try lower-length sequences first before trying length-l sequences.
        ///
        /// For example, consider a sequence that is date-repeat-dictionary.
        ///  - An attacker would need to try other date-repeat-dictionary combinations,
        ///    hence the product term.
        ///  - An attacker would need to try repeat-date-dictionary, dictionary-repeat-date,
        ///    ..., hence the factorial term.
        ///  - An attacker would also likely try length-1 (dictionary) and length-2 (dictionary-date)
        ///    sequences before length-3. assuming at minimum D guesses per pattern type,
        ///    D^(l-1) approximates Sum(D^i for i in [1..l - 1]
        /// </remarks>
        /// <param name="matches">Password being evaluated</param>
        /// <param name="password">List of matches found against the password</param>
        /// <returns>A <see cref="Result"/> object for the lowest entropy and the most guessable match sequences</returns>
        public static Result FindBestMatchSequences(string password, IEnumerable<Match> matches, bool excludeAdditive = false)
        {
            if (string.IsNullOrEmpty(password)) throw new ArgumentNullException(nameof(password));

            int bruteforceCardinality = PasswordScoring.PasswordCardinality(password);

            // Partition matches into sublists according to ending index j
            List<List<Match>> matchesByJ = Enumerable.Repeat<List<Match>>(new List<Match>(), password.Length).ToList();
            foreach (var matchGroup in matches.GroupBy(match => match.j))
            {
                // Small detail: for deterministic output, sort each sublist by i.
                matchesByJ.Insert(matchGroup.Key, matchGroup.OrderBy(m => m.i).ToList());
            }

            // Minimum entropy up to position k in the password
            double[] minimumEntropyToIndex = new double[password.Length];
            //Optimal.pi
            Match[] bestMatchForIndex = new Match[password.Length];
            //Optimal.m
            // For guesses
            Optimal optimal = new Optimal(password.Length);

            double baseBruteforceEntropy = Math.Log(bruteforceCardinality, 2);
            for (int k = 0; k < password.Length; k++)
            {
                // Start with brute-force scenario added to previous sequence to beat
                minimumEntropyToIndex[k] = (k == 0 ? 0 : minimumEntropyToIndex[k - 1]) + baseBruteforceEntropy;

                // All matches that end at the current character, test to see if the entropy is less
                foreach (Match match in matchesByJ[k])
                {
                    double candidateEntropy;
                    if (match.i > 0)
                    {
                        candidateEntropy = minimumEntropyToIndex[match.i - 1] + match.Entropy;

                        // For guesses
                        foreach (int entry in optimal.m[match.i - 1].Keys)
                        {
                            Update(match, entry + 1, optimal, excludeAdditive);
                        }
                    }
                    else
                    {
                        candidateEntropy = match.Entropy;

                        // For guesses
                        Update(match, 1, optimal, excludeAdditive);
                    }//if-else

                    // For entropy
                    if (candidateEntropy < minimumEntropyToIndex[k])
                    {
                        minimumEntropyToIndex[k] = candidateEntropy;
                        bestMatchForIndex[k] = match;
                    }
                }//foreach matchesByJ[k]

                // For guesses
                BruteforceUpdate(password, k, optimal, excludeAdditive);
            }//for k

            var (matchSequence, optimalMatchSequence) = Unwind(password.Length, bestMatchForIndex, optimal);

            // ToDo: move into for(k) loop
            // The match sequence might have gaps, fill in with brute-force matching
            // After this the matches in matchSequence must cover the whole string (i.e. match[k].j == match[k + 1].i - 1)
            if (matchSequence.Any())
            {
                // There are matches, so find the gaps and fill them in
                List<Match> matchSequenceCopy = new List<Match>();
                for (int k = 0; k < matchSequence.Count; k++)
                {
                    Match m1 = matchSequence[k];
                    // Next match, or a match past the end of the password
                    Match m2 = (k < matchSequence.Count - 1) ? matchSequence[k + 1] : new Match(0, password.Length) { i = password.Length };

                    matchSequenceCopy.Add(m1);
                    if (m1.j < m2.i - 1)
                    {
                        // Fill in gap
                        matchSequenceCopy.Add(MakeBruteforceMatch(password, i: m1.j + 1, j: m2.i - 1));
                    }
                }

                matchSequence = matchSequenceCopy;
            }
            else
            {
                // To make things easy, we'll separate out the case where there are no matches so everything is brute-forced
                matchSequence.Add(MakeBruteforceMatch(password, i: 0, j: password.Length - 1));
            }//if-else

            return new Result
            {
                Password = password,
                Entropy = minimumEntropyToIndex[password.Length - 1],
                Guesses = optimal.g[password.Length - 1][optimalMatchSequence.Count],
                EntropySequence = matchSequence,
                GuessesSequence = optimalMatchSequence,
            };
        }//FindMinimumEntropyMatch

        /// <summary>
        /// Returns the non-overlapping sequence with minimum guesses.
        /// </summary>
        /// <param name="password">Password being evaluated</param>
        /// <param name="matches">List of matches found against the password</param>
        /// <param name="excludeAdditive"></param>
        /// <returns>A <see cref="Result"/> object for the lowest entropy match sequence</returns>
        public static Result MostGuessableMatchSequence(string password, IEnumerable<Match> matches, bool excludeAdditive = false)
        {
            if (string.IsNullOrEmpty(password)) throw new ArgumentNullException(nameof(password));

            // Partition matches into sublists according to ending index j
            List<List<Match>> matchesByJ = Enumerable.Repeat<List<Match>>(new List<Match>(), password.Length).ToList();
            foreach (var matchGroup in matches.GroupBy(match => match.j))
            {
                // Small detail: for deterministic output, sort each sublist by i.
                matchesByJ.Insert(matchGroup.Key, matchGroup.OrderBy(m => m.i).ToList());
            }

            Optimal optimal = new Optimal(password.Length);
            for (int k = 0; k < password.Length; k++)
            {
                foreach (Match m in matchesByJ[k])
                {
                    if (m.i > 0)
                    {
                        foreach (int entry in optimal.m[m.i - 1].Keys)
                        {
                            Update(m, entry + 1, optimal, excludeAdditive);
                        }
                    }
                    else
                    {
                        Update(m, 1, optimal, excludeAdditive);
                    }
                }

                BruteforceUpdate(password, k, optimal, excludeAdditive);
            }

            var (_, optimalMatchSequence) = Unwind(password.Length, null, optimal: optimal);

            return new Result
            {
                Password = password,
                Guesses = password.Length == 0 ? 1 : optimal.g[password.Length - 1][optimalMatchSequence.Count],
                GuessesSequence = optimalMatchSequence
            };
        }//MostGuessableMatchSequence

        // Helper: considers whether a length-l sequence ending at match m is better (fewer guesses)
        // than previously encountered sequences, updating state if so.
        private static void Update(Match m, int l, Optimal optimal, bool excludeAdditive)
        {
            int k = m.j;

            double pi = m.Guesses;

            // l = optimal.m[m.i - 1] + 1 or 1 if m <= 0
            // We're considering a length-l sequence ending with match m:
            // obtain the product term in the minimization function by multiplying m's guesses
            // by the product of the length-(l-1) sequence ending just before m, at m.i - 1.
            if (l > 1) pi *= optimal.pi[m.i - 1][l - 1];
            if (double.IsInfinity(pi)) pi = double.MaxValue;

            // Calculate the minimization function
            double g = Factorial(l) * pi;
            if (double.IsInfinity(g)) g = double.MaxValue;
            if (!excludeAdditive)
            {
                g += Math.Pow(MinGuessesBeforeGrowingSequence, l - 1);
                if (double.IsInfinity(g)) g = double.MaxValue;
            }

            // Update state if new best.
            // First see if any competing sequences covering this prefix, with l or fewer matches,
            // fare better than this sequence. if so, skip it and return.
            foreach (var competing in optimal.g[k])
            {
                if (competing.Key <= l && competing.Value <= g) return;
            }

            // This sequence might be part of the final optimal sequence.
            optimal.g[k][l] = g;
            optimal.m[k][l] = m;
            optimal.pi[k][l] = pi;
        }//Update

        // Unoptimized, called only on small n
        private static int Factorial(int n)
        {
            int f = 1;
            for (int i = 1; i <= n; i++) f *= i;

            return f;
        }//Factorial

        // Helper: step backwards through optimal.m starting at the end,
        // constructing the final optimal match sequence.
        private static (List<Match> Entropy, List<Match> Guesses) Unwind(int n, Match[] bestMatches, Optimal optimal)
        {
            #region Entropy
            List<Match> entropyMatchSequence = null;
            if (bestMatches != null)
            {
                // Walk backwards through lowest entropy matches, to build the best password sequence
                entropyMatchSequence = new List<Match>(bestMatches.Length);
                for (int kv = n - 1; kv >= 0; kv--)
                {
                    if (bestMatches[kv] != null)
                    {
                        entropyMatchSequence.Insert(0, bestMatches[kv]);
                        kv = bestMatches[kv].i; // Jump back to start of match
                    }
                }
            }
            #endregion Entropy

            List<Match> optimalMatchSequence = new List<Match>();
            int k = n - 1;
            // Find the final best sequence length and score
            (int l, double g) = optimal.g[k].Aggregate((l: int.MinValue, g: double.PositiveInfinity),
                (best, candidate) => candidate.Value < best.g ? (l: candidate.Key, g: candidate.Value) : best);

            while (k >= 0)
            {
                Match m = optimal.m[k][l];
                optimalMatchSequence.Insert(0, m);
                k = m.i - 1;
                l--;
            }

            return (Entropy: entropyMatchSequence, Guesses: optimalMatchSequence);
        }//Unwind

        // Helper: evaluate brute-force matches ending at k.
        private static void BruteforceUpdate(string password, int k, Optimal optimal, bool excludeAdditive)
        {
            // See if a single brute-force match spanning the k-prefix is optimal.
            Match m = MakeBruteforceMatch(password, 0, k);
            Update(m, 1, optimal, excludeAdditive);
            for (int i = 1; i <= k; i++)
            {
                // Generate k brute-force matches, spanning from (i = 1, j = k) up to (i = k, j = k).
                // See if adding these new matches to any of the sequences in optimal[i - 1] leads to new bests.
                m = MakeBruteforceMatch(password, i, k);
                foreach (var entry in optimal.m[i - 1])
                {
                    int l = entry.Key;
                    Match last_m = entry.Value;

                    // Corner: an optimal sequence will never have two adjacent brute-force matches.
                    // It is strictly better to have a single brute-force match spanning the same region:
                    // same contribution to the guess product with a lower length.
                    // --> safe to skip those cases.
                    if (last_m.Pattern == Pattern.Bruteforce) continue;

                    // Try adding m to this length - l sequence.
                    else Update(m, l + 1, optimal, excludeAdditive);
                }//foreach
            }//for
        }//BruteforceUpdate

        // Helper: make brute-force match objects spanning i to j, inclusive.
        private static Match MakeBruteforceMatch(string password, int i, int j)
        {
            int tokenLength = j - i + 1;
            string token = password.Substring(i, tokenLength);
            var (entropy, guesses) = CalculateBruteforceVariations(token, tokenLength);

            return new Match(tokenLength, password.Length)
            {
                Pattern = Pattern.Bruteforce,
                i = i,
                j = j,
                Token = token,
                Cardinality = BruteforceCardinality,
                Entropy = entropy,
                Guesses = guesses
            };
        }//MakeBruteforceMatch

        private static (double Entropy, double Guesses) CalculateBruteforceVariations(string token, int tokenLength)
        {
            double guesses = Math.Pow(PasswordScoring.PasswordCardinality(token), tokenLength);
            if (double.IsPositiveInfinity(guesses)) guesses = double.MaxValue;

            // Small detail: make brute-force matches at minimum one guess bigger than smallest allowed
            // sub-match guesses, such that non-brute-force sub-matches over the same [i..j] take precedence.
            double minGuesses = (tokenLength == 1 ? MinSubmatchGuessesSingleChar : MinSubmatchGuessesMultiChar) + 1;

            return (Entropy: Math.Log(guesses, 2), Guesses: Math.Max(guesses, minGuesses));
        }//CalculateBruteforceVariations

        private class Optimal
        {
            // Optimal.m[k][l] holds final match in the best length-l match sequence covering
            // the password prefix up to k, inclusive.
            // if there is no length-l sequence that scores better (fewer guesses) than
            // a shorter match sequence spanning the same prefix, optimal.m[k][l] is undefined.
            public readonly List<Dictionary<int, Match>> m = new List<Dictionary<int, Match>>();

            // Same structure as Optimal.m -- holds the product term Prod(m.Guesses for m in sequence).
            // Optimal.pi allows for fast (non-looping) updates to the minimization function.
            public readonly List<Dictionary<int, double>> pi = new List<Dictionary<int, double>>();

            // Same structure as optimal.m -- holds the overall metric.
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
