using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Zxcvbn.Matcher
{
    public class Matching
    {
        protected Dictionary<string, Dictionary<string, int>> RankedDictionaries { get; }

        private static readonly IEnumerable<(string Key, IEnumerable<string> Value)> BaseRankedDictionaries =
            new KeyValuePair<string, string>[]
            {
                new KeyValuePair<string, string>("password", Properties.Resources.Passwords),
                new KeyValuePair<string, string>("english", Properties.Resources.English),
                new KeyValuePair<string, string>("male_names", Properties.Resources.MaleNames),
                new KeyValuePair<string, string>("female_names", Properties.Resources.FemaleNames),
                new KeyValuePair<string, string>("surnames", Properties.Resources.Surnames)
            }.Select(dict => (dict.Key, Value: dict.Value.Split(new string[] { "\n" }, StringSplitOptions.RemoveEmptyEntries).AsEnumerable()));

        public Matching(in IEnumerable<string> orderedList = null) : this(BaseRankedDictionaries, orderedList)
        {
        }

        public Matching(IEnumerable<(string Key, IEnumerable<string> Value)> dictionaries, in IEnumerable<string> orderedList)
        {
            if (orderedList != null && orderedList.Any())
            {
               dictionaries = dictionaries.Concat(new (string Key, IEnumerable<string> Value)[] { (Key: "user_inputs", Value: orderedList) });
            }

            RankedDictionaries = dictionaries.ToDictionary(dict => dict.Key, dict => BuildRankedDictionary(dict.Value));
        }//Matching(IEnumerable<(string Key, IEnumerable<string> Value)> dictionaries, in IEnumerable<string> orderedList)

        protected Matching(Dictionary<string, Dictionary<string, int>> rankedDictionaries, in IEnumerable<string> orderedList)
        {
            // Null and empty check for rankedDictionaries
            RankedDictionaries = (rankedDictionaries != null && rankedDictionaries.Any() && rankedDictionaries.Values.Any()) ?
                rankedDictionaries : new Dictionary<string, Dictionary<string, int>>();

            // Null and empty check for orderedList
            if (orderedList != null && orderedList.Any()) RankedDictionaries.Add("user_inputs", BuildRankedDictionary(orderedList));
        }//Matching(Dictionary<string, Dictionary<string, int>> rankedDictionaries, IEnumerable<string> orderedList)

        /// <summary>
        /// Set (or add if not exist) a new dictionary.
        /// <paramref name="wordListPath"/> must be the path (relative or absolute)
        /// to a file containing one word per line, entirely in lowercase, ordered by frequency (decreasing).
        /// </summary>
        /// <param name="name">The name provided to the dictionary used</param>
        /// <param name="wordListPath">The filename of the dictionary (full or relative path)</param>
        public void SetDictionary(string name, string wordListPath) =>
            RankedDictionaries[name] = BuildRankedDictionary(File.ReadAllLines(wordListPath));

        /// <summary>
        /// Set (or add if not exist) a new dictionary from the passed in word list.
        /// If there is any frequency order then they should be in decreasing frequency order.
        /// </summary>
        public void SetDictionary(string name, in IEnumerable<string> wordList)
        {
            if (wordList == null || !wordList.Any()) throw new ArgumentNullException(nameof(wordList));
            RankedDictionaries[name] = BuildRankedDictionary(wordList);
        }//SetDictionary(string name, in IEnumerable<string> wordList)

        public void SetUserInputDictionary(in IEnumerable<string> orderedList) => SetDictionary("user_inputs", orderedList);

        protected static Dictionary<string, int> BuildRankedDictionary(in IEnumerable<string> wordList)
        {
            // The word list is assumed to be in increasing frequency order
            // Must ensure that the dictionary is using lowercase words only
            return wordList.AsParallel()
                           .Select((word, i) => (Key: word.ToLowerInvariant(), Value: i + 1))  // Rank starts at 1, not 0
                           .ToDictionary(kv => kv.Key, kv => kv.Value);
        }//BuildRankedDictionary
    }
}
