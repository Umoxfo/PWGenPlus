﻿using System;
using System.Collections.Generic;
using System.Linq;

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
    /// <para>See <see cref="IMatcher"/> and the classes that implement it for more information on each kind of pattern matcher.</para>
    /// </summary>
    class DefaultMatcherFactory : IMatcherFactory
    {
        List<IMatcher> matchers;

        /// <summary>
        /// Create a matcher factory that uses the default list of pattern matchers
        /// </summary>
        public DefaultMatcherFactory()
        {
            var dictionaryMatchers = new List<DictionaryMatcher>() {
                new DictionaryMatcher("passwords",
                    Properties.Resources.Passwords.Split(new string[] { "\r\n" }, StringSplitOptions.RemoveEmptyEntries)),
                new DictionaryMatcher("english",
                    Properties.Resources.English.Split(new string[] { "\r\n" }, StringSplitOptions.RemoveEmptyEntries)),
                new DictionaryMatcher("male_names",
                    Properties.Resources.MaleNames.Split(new string[] { "\r\n" }, StringSplitOptions.RemoveEmptyEntries)),
                new DictionaryMatcher("female_names",
                    Properties.Resources.FemaleNames.Split(new string[] { "\r\n" }, StringSplitOptions.RemoveEmptyEntries)),
                new DictionaryMatcher("surnames",
                    Properties.Resources.Surnames.Split(new string[] { "\r\n" }, StringSplitOptions.RemoveEmptyEntries)),
            };

            matchers = new List<IMatcher> {
                new RepeatMatcher(),
                new SequenceMatcher(),
                new RegexMatcher("\\d{3,}", 10, true, "digits"),
                new RegexMatcher("19\\d\\d|200\\d|201\\d", 119, false, "year"),
                new DateMatcher(),
                new SpatialMatcher()
            };

            matchers.AddRange(dictionaryMatchers);
            matchers.Add(new L33tMatcher(dictionaryMatchers));
        }

        /// <summary>
        /// Get instances of pattern matchers, adding in per-password matchers on userInputs (and userInputs with l33t substitutions)
        /// </summary>
        /// <param name="userInputs">Enumerable of user information</param>
        /// <returns>Enumerable of matchers to use</returns>
        public IEnumerable<IMatcher> CreateMatchers(IEnumerable<string> userInputs)
        {
            var userInputDict = new DictionaryMatcher("user_inputs", userInputs);
            var leetUser = new L33tMatcher(userInputDict);

            return matchers.Union(new List<IMatcher> { userInputDict, leetUser });
        }
    }
}
