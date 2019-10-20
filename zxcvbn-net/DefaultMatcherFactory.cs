using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text.RegularExpressions;
using Zxcvbn.Matcher;

namespace Zxcvbn
{
    /// <summary>
    /// <para>This matcher factory will use all of the default password matchers.</para>
    ///
    /// <para>Default dictionary matchers use the built-in word lists:
    /// passwords, english, male_names, female_names, surnames</para>
    /// <para>Also matching against: user data, all dictionaries with l33t substitutions</para>
    /// <para>Other default matchers: repeats, sequences, digits, years, dates, spatial</para>
    ///
    /// <para>See <see cref="IMatcher"/> and the classes that implement it
    /// for more information on each kind of pattern matcher.</para>
    /// </summary>
    internal class DefaultMatcherFactory : Matching, IMatcherFactory
    {
        private static readonly IEnumerable<(string Key, IEnumerable<string> Value)> BaseRankedDictionaries =
            new KeyValuePair<string, string>[]
            {
                new KeyValuePair<string, string>("password", Properties.Resources.Passwords),
                new KeyValuePair<string, string>("english", Properties.Resources.English),
                new KeyValuePair<string, string>("male_names", Properties.Resources.MaleNames),
                new KeyValuePair<string, string>("female_names", Properties.Resources.FemaleNames),
                new KeyValuePair<string, string>("surnames", Properties.Resources.Surnames)
            }.Select(dict => (dict.Key, Value: dict.Value.Split(new char[] { '\n' }, StringSplitOptions.RemoveEmptyEntries).AsEnumerable()));

        private static readonly ReadOnlyDictionary<string, RegexMatcherInfo> Regexen =
            new ReadOnlyDictionary<string, RegexMatcherInfo>(new Dictionary<string, RegexMatcherInfo>()
            {
                ["digits"] = new RegexMatcherInfo(new Regex(@"\d{3,}", RegexOptions.Compiled), 10, true),
                ["recent_year"] = new RegexMatcherInfo(new Regex(@"19\d{2}|20[0-2]\d", RegexOptions.Compiled), 119, false),
            });

        private readonly List<IMatcher> matchers;

        /// <summary>
        /// Create a matcher factory that uses the default list of pattern matchers and userInputs
        /// </summary>
        public DefaultMatcherFactory(IEnumerable<string> userInputs = null) : base(BaseRankedDictionaries, userInputs)
        {
            DictionaryMatcher dictionaryMatcher = new DictionaryMatcher(RankedDictionaries);

            matchers = new List<IMatcher> {
                dictionaryMatcher,
                new ReversedDictionaryMatcher(dictionaryMatcher),
                new L33tMatcher(dictionaryMatcher),
                new SpatialMatcher(),
                new RepeatMatcher(),
                new SequenceMatcher(),
                new RegexMatcher(Regexen),
                new DateMatcher(),
            };
        }

        /// <summary>
        /// Match password against the combined matchers that are
        /// dictionary (also reverse and with L33t substitutions), spatial, repeats, sequences, RegEx, and data.
        /// </summary>
        /// <param name="password">The password to match</param>
        /// <returns>An enumerable of combine matches</returns>
        public IEnumerable<Match> Omnimatch(string password) =>
            matchers.SelectMany(matcher => matcher.MatchPassword(password)).OrderBy(m => m);
    }
}
