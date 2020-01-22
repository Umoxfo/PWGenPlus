using System;
using System.Collections.Generic;
using System.Linq;

namespace Umoxfo.Zxcvbn.Matcher
{
    /// <summary>
    /// <para>A matcher that checks for keyboard layout patterns (e.g. 78523 on a keypad, or plkmn on a QWERTY keyboard).</para>
    /// <para>Has patterns for QWERTY, DVORAK, JIS, numeric keypad, and mac numeric keypad</para>
    /// <para>The matcher accounts for shifted characters (e.g. qwErt or po9*7y)
    /// when detecting patterns as well as multiple changes in direction.</para>
    /// </summary>
    public class SpatialMatcher : IMatcher
    {
        private readonly Lazy<List<SpatialGraph>> spatialGraphs;

        public SpatialMatcher() => spatialGraphs = new Lazy<List<SpatialGraph>>(() => GenerateSpatialGraphs());

        public SpatialMatcher(string name, string layout, bool slanted) =>
            spatialGraphs = new Lazy<List<SpatialGraph>>(() => new List<SpatialGraph> { new SpatialGraph(name, layout, slanted) });

        public SpatialMatcher(params SpatialGraph[] spatials) =>
            spatialGraphs = new Lazy<List<SpatialGraph>>(() => spatials.ToList());

        public void AddSpatialGraphs(string name, string layout, bool slanted) =>
            spatialGraphs.Value.Add(new SpatialGraph(name, layout, slanted));

        /// <summary>
        /// Match the password against the known keyboard layouts
        /// </summary>
        /// <param name="password">The password to match</param>
        /// <returns>List of matching patterns</returns>
        /// <seealso cref="SpatialMatch"/>
        public IEnumerable<Match> MatchPassword(string password) =>
            spatialGraphs.Value.SelectMany(g => SpatialMatch(g, password)).OrderBy(m => m);

        /// <summary>
        /// Match the password against a single pattern
        /// </summary>
        /// <param name="graph">Adjacency graph for this key layout</param>
        /// <param name="password">The password to match</param>
        /// <returns>List of matching patterns</returns>
        private IEnumerable<SpatialMatch> SpatialMatch(SpatialGraph graph, string password)
        {
            int lastPaswordIndex = password.Length - 1;

            int i = 0;
            while (i < lastPaswordIndex)
            {
                int turns = 0, shiftedCount = 0;
                int lastDirection = -1;

                int j = i + 1;
                for (; j < password.Length; j++)
                {
                    (int foundDirection, bool shifted) = graph.GetAdjacentCharDirection(password[j - 1], password[j]);

                    if (foundDirection != -1)
                    {
                        // Spatial match continues
                        if (shifted) shiftedCount++;
                        if (lastDirection != foundDirection)
                        {
                            // Adding a turn is correct even in the initial case when lastDirection is null:
                            // every spatial pattern starts with a turn.
                            turns++;
                            lastDirection = foundDirection;
                        }
                    }
                    else break; // This character not a spatial match
                }

                // Only consider runs of greater than two
                if (j - i > 2)
                {
                    yield return new SpatialMatch(tokenLength: j - i, password.Length)
                    {
                        Pattern = Pattern.Spatial,
                        i = i,
                        j = j - 1,
                        Token = password.Substring(i, j - i),
                        Graph = graph.Name,
                        Turns = turns,
                        ShiftedCount = shiftedCount,
                        Entropy = graph.CalculateEntropy(j - i, turns, shiftedCount),
                        Guesses = graph.CalculateGuesses(j - i, turns, shiftedCount)
                    };
                }

                i = j;
            }//while
        }//SpatialMatch

        // In the JS version these are precomputed, but for now we'll generate them here when they are first needed.
        private static List<SpatialGraph> GenerateSpatialGraphs()
        {
            // Keyboard layouts directly from zxcvbn's build_keyboard_adjacency_graph.py script
            return new List<SpatialGraph>
            {
                new SpatialGraph("qwerty", Properties.Resources.QWERTY, true),
                new SpatialGraph("dvorak", Properties.Resources.DVORAK, true),
                new SpatialGraph("jis", Properties.Resources.JIS, true),
                new SpatialGraph("keypad", Properties.Resources.Keypad, false),
                new SpatialGraph("mac_keypad", Properties.Resources.mac_Keypad, false)
            };
        }//GenerateSpatialGraphs
    }//SpatialMatcher

    // See build_keyboard_adjacency_graph.py in zxcvbn for how these are generated
    public class SpatialGraph
    {
        public string Name { get; private set; }
        private Dictionary<char, List<string>> AdjacencyGraph { get; set; }// = new Dictionary<char, List<string>>();
        public int StartingPositions { get; private set; }
        public double AverageDegree { get; private set; }

        public SpatialGraph(string name, string layout, bool slanted)
        {
            Name = name;
            BuildGraph(layout ?? string.Empty, slanted);
        }

        /// <summary>
        /// Returns true when testAdjacent is in c's adjacency list
        /// </summary>
        public bool IsCharAdjacent(char c, char testAdjacent)
        {
            return AdjacencyGraph.TryGetValue(c, out List<string> adjacencies) ?
                adjacencies.Any(s => s.Contains(testAdjacent)) : false;
        }

        /// <summary>
        /// Returns the 'direction' of the adjacent character (i.e. index in the adjacency list).
        /// If the character is not adjacent, -1 is returned
        /// </summary>
        /// <param name="c">Character</param>
        /// <param name="adjacent">Adjacent character</param>
        /// <returns>A tuple for the direction of the adjacent character and
        /// whether the matching character is shifted.</returns>
        public (int, bool) GetAdjacentCharDirection(char c, char adjacent)
        {
            if (!AdjacencyGraph.TryGetValue(c, out List<string> adjChrList)) return (-1, false);

            string adjacentEntry = adjChrList.FirstOrDefault(s => s != null && s.Contains(adjacent));
            if (string.IsNullOrEmpty(adjacentEntry)) return (-1, false);

            // Index 1 in the adjacency means the key is shifted,
            // 0 means unshifted: A vs a, % vs 5, etc.
            // for example,
            //  'q' is adjacent to the entry '2@'.
            //  @ is shifted w/ index 1, 2 is unshifted.
            bool shifted = adjacentEntry.IndexOf(adjacent) > 0; // i.e. shifted if not first character in the adjacency
            return (adjChrList.IndexOf(adjacentEntry), shifted);
        }//GetAdjacentCharDirection

        /*
         * Returns the six adjacent coordinates on a standard keyboard, where each row is slanted to
         * the right from the last.adjacencies are clockwise,
         * starting with key to the left, then two keys above, then right key, then two keys below.
         * (that is, only near-diagonal keys are adjacent,
         * so g's coordinate is adjacent to those of t,y,b,v, but not those of r,u,n,c.)
         */
        private static Point[] GetSlantedAdjacent(int x, int y)
        {
            return new Point[]
            {
                new Point(x - 1, y),
                new Point(x, y - 1),
                new Point(x + 1, y - 1),
                new Point(x + 1, y),
                new Point(x, y + 1),
                new Point(x - 1, y + 1)
            };
        }

        // Returns the nine clockwise adjacent coordinates on a keypad, where each row is vertical-align.
        private static Point[] GetAlignedAdjacent(int x, int y)
        {
            return new Point[]
            {
                new Point(x - 1, y),
                new Point(x - 1, y - 1),
                new Point(x, y - 1),
                new Point(x + 1, y - 1),
                new Point(x + 1, y),
                new Point(x + 1, y + 1),
                new Point(x, y + 1),
                new Point(x - 1, y + 1)
            };
        }

        /*
         * Builds an adjacency graph as a dictionary: {character: [adjacent_characters]}.
         * adjacent characters occur in a clockwise order.
         * for example:
         *  * on qwerty layout, 'g' maps to ['fF', 'tT', 'yY', 'hH', 'bB', 'vV']
         *  * on keypad layout, '7' maps to [None, None, None, '=', '8', '5', '4', None]
         */
        private void BuildGraph(string layout, bool slanted)
        {
            string[] tokens = layout.Split((char[])null, StringSplitOptions.RemoveEmptyEntries);
            int tokenSize = tokens[0].Length;

            // Put the characters in each keyboard cell into the map again t their coordinates
            string[] lines = layout.Split("\n".ToCharArray(), StringSplitOptions.RemoveEmptyEntries);

            Dictionary<(int x, int y), string> positionTable = lines.SelectMany((line, y) =>
            {
                int slant = slanted ? y : 0;

                return from token in line.Split((char[])null, StringSplitOptions.RemoveEmptyEntries)
                       let x = (line.IndexOf(token, StringComparison.InvariantCulture) - slant) / (tokenSize + 1)
                       select (Key: (x, y), Value: token);
            }).ToDictionary(kv => kv.Key, kv => kv.Value);

            #region for loop
            /*
            Dictionary<Point, string> positionTable = new Dictionary<Point, string>();
            for (int y = 0; y < lines.Length; ++y)
            {
                string line = lines[y];
                int slant = slanted ? y - 1 : 0;

                foreach (string token in line.Split((char[])null, StringSplitOptions.RemoveEmptyEntries))
                {
                    int x = (line.IndexOf(token, StringComparison.InvariantCulture) - slant) / (tokenSize + 1);
                    Point p = new Point(x, y);
                    positionTable[p] = token;
                }
            }
            */
            #endregion

            AdjacencyGraph =
                (from pair in positionTable
                 from c in pair.Value
                 let adjacentPoints = slanted ? GetSlantedAdjacent(pair.Key.x, pair.Key.y) : GetAlignedAdjacent(pair.Key.x, pair.Key.y)
                 select (Key: c, Value: adjacentPoints.Select(adj => positionTable.TryGetValue((adj.x, adj.y), out string val) ? val : null).ToList())
                ).ToDictionary(kv => kv.Key, kv => kv.Value);

            #region foreach
            /*
            foreach (var pair in positionTable)
            {
                (int x, int y) = pair.Key;
                foreach (char c in pair.Value)
                {
                    Point[] adjacentPoints = slanted ? GetSlantedAdjacent(x, y) : GetAlignedAdjacent(x, y);
                    // We want to include nulls so that direction is correspondent with index in the list
                    AdjacencyGraph[c] = adjacentPoints.Select(adj => positionTable.TryGetValue((adj.x, adj.y), out string val) ? val : null).ToList();
                }
            }
            */
            #endregion

            // Calculate average degree and starting positions, cf. init.coffee
            StartingPositions = AdjacencyGraph.Count;
            AverageDegree = AdjacencyGraph.Sum(adj => adj.Value.Count(a => a != null)) * 1.0 / StartingPositions;
        }//BuildGraph

        /// <summary>
        /// Calculate guesses for a math that was found on this adjacency graph
        /// </summary>
        public double CalculateGuesses(int matchLength, int turns, int shiftedCount)
        {
            double guesses = 0;
            // Estimate the number of possible patterns with the match length or less with the number of turns or less.
            for (int i = 2; i <= matchLength; i++)
            {
                int possibleTurns = Math.Min(turns, i - 1);
                for (int j = 1; j <= possibleTurns; j++)
                {
                    guesses += PasswordScoring.Binomial(i - 1, j - 1) * StartingPositions * Math.Pow(AverageDegree, j);
                }
            }

            // Add extra guesses for shifted keys. (% instead of 5, A instead of a.)
            // Math is similar to extra guesses of l33t substitutions in dictionary matches.
            if (shiftedCount > 0)
            {
                int unshiftedCount = matchLength - shiftedCount;
                if (shiftedCount == 0 || unshiftedCount == 0)
                {
                    guesses *= 2;
                }
                else
                {
                    long shiftedVariations = 0;
                    int limitCount = Math.Min(shiftedCount, unshiftedCount);
                    for (int i = 1; i <= limitCount; i++)
                    {
                        shiftedVariations += PasswordScoring.Binomial(matchLength, i);
                    }
                    guesses *= shiftedVariations;
                }
            }

            return guesses;
        }//CalculateGuesses

        /// <summary>
        /// Calculate entropy for a math that was found on this adjacency graph
        /// </summary>
        public double CalculateEntropy(int matchLength, int turns, int shiftedCount)
        {
            // This is an estimation of the number of patterns with length of matchLength or less with 'turns' turns or less
            double possibilities = Enumerable.Range(2, matchLength - 1).Sum(i =>
                Enumerable.Range(1, Math.Min(turns, i - 1))
                          .Sum(j => StartingPositions * Math.Pow(AverageDegree, j) * PasswordScoring.Binomial(i - 1, j - 1)));

            double entropy = Math.Log(possibilities, 2);

            // Entropy increases for a mix of shifted and unshifted
            if (shiftedCount > 0)
            {
                int unshifted = matchLength - shiftedCount;
                entropy += Math.Log(Enumerable.Range(0, Math.Min(shiftedCount, unshifted) + 1)
                                              .Sum(i => PasswordScoring.Binomial(matchLength, i)), 2);
            }

            return entropy;
        }//CalculateEntropy

        // Instances of Point or Pair in the standard library are in UI assemblies,
        // so define our own version to reduce dependencies
        private readonly struct Point
        {
            public readonly int x;
            public readonly int y;

            public Point(int x, int y) => (this.x, this.y) = (x, y);
        }
    }//SpatialGraph

    /// <summary>
    /// A match made with the <see cref="SpatialMatcher"/>
    /// that contains some additional information specific to the spatial match.
    /// </summary>
    public class SpatialMatch : Match
    {
        public SpatialMatch(int tokenLength, int passwordLength) : base(tokenLength, passwordLength)
        {
        }

        /// <summary>
        /// The name of the keyboard layout used to make the spatial match
        /// </summary>
        public string Graph { get; set; }

        /// <summary>
        /// The number of turns made (i.e. when direction of adjacent keys changes)
        /// </summary>
        public int Turns { get; set; }

        /// <summary>
        /// The number of shifted characters matched in the pattern (adds to entropy)
        /// </summary>
        public int ShiftedCount { get; set; }
    }
}
