using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Linq;
using System.Globalization;
using Zxcvbn.Utils;
using Zxcvbn.Report;

namespace Zxcvbn.Matcher
{
    /// <summary>
    /// A "date" is recognized as:
    /// <list type="bullet">
    ///     <item>
    ///         <description>Any 3-tuple that starts or ends with a 2- or 4-digit year</description>
    ///         <list type="bullet">
    ///             <item><description>With 2 or 0 separator chars (1.1.91 or 1191)</description></item>
    ///             <item><description>Maybe zero-padded (01-01-91 vs 1-1-91)</description></item>
    ///         </list>
    ///     </item>
    ///     <item>
    ///         <description>A year is from 1 to the current year and is represented by 2 or 4 digits</description>
    ///     </item>
    ///     <item>
    ///         <description>A month between 1 and 12</description>
    ///     </item>
    ///     <item>
    ///         <description>A day between 1 and 31</description>
    ///     </item>
    /// </list>
    ///
    /// The detected dates are verified based on the Gregorian calendar.
    /// </summary>
    /// <remarks>
    /// Note: instead of using a lazy or greedy regular expression for the entire string,
    /// perform an exact match on all substrings of the password to obtain all possible date matches.
    /// </remarks>
    public class DateMatcher : IMatcher
    {
        private const RegexOptions options = RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.IgnorePatternWhitespace;

        private static readonly Regex DateNoSeparator = new Regex(@"^\d{4,8}$", options);

        private static readonly Dictionary<int, IEnumerable<(int k, int l, int m)>> DateSplits = new Dictionary<int, IEnumerable<(int k, int l, int m)>>
        {
            {
                4, new []
                   {
                    (k: 1, l: 1, m: 2), // e.g. 1 1 91
                    (k: 2, l: 1, m: 1)  // e.g. 91 1 1
                   }
            },
            {
                5, new []
                   {
                    (k: 1, l: 1, m: 3), // e.g. 1 1 911
                    (k: 1, l: 2, m: 2), // e.g. 1 11 91
                    (k: 2, l: 1, m: 2), // e.g. 11 1 91
                    (k: 2, l: 2, m: 1), // e.g. 91 11 1
                    (k: 3, l: 1, m: 1)  // e.g. 911 1 1
                   }
            },
            {
                6, new []
                   {
                     (k: 1, l: 1, m: 4), // e.g. 1 1 1991
                     (k: 1, l: 2, m: 3), // e.g. 1 11 911
                     (k: 2, l: 1, m: 3), // e.g. 11 1 911
                     (k: 2, l: 2, m: 2), // e.g. 11 11 91
                     (k: 3, l: 1, m: 2), // e.g. 911 1 11
                     (k: 3, l: 2, m: 1), // e.g. 911 11 1
                     (k: 4, l: 1, m: 1)  // e.g. 1991 1 1
                   }
            },
            {
                7, new []
                   {
                     (k: 1, l: 2, m: 4), // e.g. 1 11 1991
                     (k: 2, l: 1, m: 4), // e.g. 11 1 1991
                     (k: 2, l: 2, m: 3), // e.g. 11 11 991
                     (k: 3, l: 2, m: 2), // e.g. 991 11 11
                     (k: 4, l: 1, m: 2), // e.g. 1991 1 11
                     (k: 4, l: 2, m: 1), // e.g. 1991 11 1
                   }
            },
            {
                8, new []
                   {
                     (k: 2, l: 2, m: 4), // e.g. 11 11 1991
                     (k: 4, l: 2, m: 2)  // e.g. 1991 11 11
                   }
            },
        };

        private static readonly ((int year, int month, int day) date, int distance) CandidateSeed =
            (date: (DateTime.MinValue.Year, DateTime.MinValue.Month, DateTime.MinValue.Day), distance: int.MaxValue);

        private const string DateYearWithSeparatorPattern = @"^(?<month_or_day>\d{1,2})(?<separator>[\s-/\\_\.])(?<day_or_month>\d{1,2})\k<separator>(?<year>\d{2,4})$";
        private const string YearDateWithSeparatorPattern = @"^(?<year>\d{2,4})(?<separator>[\s-/\\_\.])(?<month_or_day>\d{1,2})\k<separator>(?<day_or_month>\d{1,2})$";

        private static readonly IReadOnlyList<Regex> dateWithSeparatorRegexes = new List<Regex>
        {
            new Regex(DateYearWithSeparatorPattern, options),
            new Regex(YearDateWithSeparatorPattern, options)
        }.AsReadOnly();

        private static readonly int DateMinYear = DateTime.MinValue.Year;
        private static readonly int DateMaxYear = DateTime.MaxValue.Year;

        // Uses the default calendar of the InvariantCulture.
        private static readonly Calendar calendar = CultureInfo.InvariantCulture.Calendar;

        /// <summary>
        /// Find date matches in <paramref name="password"/>
        /// </summary>
        /// <param name="password">The password to match</param>
        /// <returns>An enumerable of date matches</returns>
        /// <seealso cref="DateMatch"/>
        public IEnumerable<Match> MatchPassword(string password)
        {
            if (string.IsNullOrEmpty(password)) throw new ArgumentNullException(nameof(password));

            var matches = SeparatorLessDateMatch(password).Concat(DelimitedDateMatch(password));

            return from match in matches
                   where matches.TakeWhile(om => match.Equals(om) || om.i > match.i || om.j < match.j).Any()
                   orderby match
                   select match;

            #region Full LINQ
            /*            return from match in matches
                               where (from otherMatch in matches
                                      where match.Equals(otherMatch) || otherMatch.i > match.i || otherMatch.j < match.j
                                      select otherMatch).Any()
                               orderby match
                               select match;*/
            #endregion
        }//MatchPassword

        private IEnumerable<DateMatch> SeparatorLessDateMatch(string password)
        {
            #region LINQ
            int lastMatchingIndex = password.Length - 3;

            return from i in Enumerable.Range(0, lastMatchingIndex)
                   from len in Enumerable.Range(4, Math.Min(5, lastMatchingIndex - i))
                   let dateMatch = DateNoSeparator.Match(password, i, len)
                   where dateMatch.Success

                   let candidate = GetDate(dateMatch)
                   where candidate != CandidateSeed.date

                   select new DateMatch
                   {
                       Pattern = Pattern.Date,
                       Token = dateMatch.Value,
                       i = i,
                       j = i + len - 1,
                       Separator = string.Empty,
                       Year = candidate.year,
                       Month = candidate.month,
                       Day = candidate.day,
                       Entropy = CalculateEntropy(candidate.year, false)
                   };
            #endregion

            #region index for loop
            /*
            int lastMatchingIndex = password.Length - 4;
            for (int i = 0; i <= lastMatchingIndex; i++)
            {
                for (int len = 4; len <= 8; len++)
                {
                    if ((i + len) > password.Length) break;

                    System.Text.RegularExpressions.Match dateMatch = DateNoSeparator.Match(password, i, len);
                    if (!dateMatch.Success) continue;

                    var candidates =
                        from split in DateSplits[dateMatch.Length]
                        let date = MapIntsToDate(dateMatch.Value.Substring(0, split.k).ToInt(),
                                                    dateMatch.Value.Substring(split.k, split.l).ToInt(),
                                                    dateMatch.Value.Substring(split.k + split.l, split.m).ToInt())
                        where date != (0, 0, 0)
                        select (date, distance: Metric(date.year, date.month, date.day));
                    if (!candidates.Any()) continue;

                    // At this point: different possible date mappings for the same i, j substring (token).
                    // Match the candidate date that likely takes the fewest guesses:
                    //  a date closest to the current date.
                    (int year, int month, int day) =
                        candidates.Aggregate((date: (year: 0, month: 0, day: 0), distance: int.MaxValue),
                                                (best, next) => next.distance < best.distance ? next : best,
                                                (best) => (best.date.year, best.date.month, best.date.day));

                    yield return new DateMatch
                    {
                        Pattern = Pattern.Date,
                        Token = dateMatch.Value,
                        i = i,
                        j = i + len - 1,
                        Separator = string.Empty,
                        Year = year,
                        Month = month,
                        Day = day,
                        Entropy = CalculateEntropy(year, false)
                    };
                }//for
            }//for
            */
            #endregion

            (int year, int month, int day) GetDate(System.Text.RegularExpressions.Match dateMatch)
            {
                return (from split in DateSplits[dateMatch.Length]
                        let date = MapIntsToDate(dateMatch.Value.Substring(0, split.k).ToInt(),
                                                 dateMatch.Value.Substring(split.k, split.l).ToInt(),
                                                 dateMatch.Value.Substring(split.k + split.l, split.m).ToInt())
                        where date != (0, 0, 0)
                        select (date, distance: Metric(date.year, date.month, date.day))

                         // At this point: different possible date mappings for the same i, j substring (token).
                         // Match the candidate date that likely takes the fewest guesses:
                         //  a date closest to the current date.
                       ).Aggregate(CandidateSeed,
                                   (best, next) => next.distance < best.distance ? next : best,
                                   (best) => best.date);
            }//GetDate
        }//SlashLessDateMatch

        private IEnumerable<DateMatch> DelimitedDateMatch(string password)
        {
            int lastMatchingIndex = password.Length - 5;

            return from i in Enumerable.Range(0, lastMatchingIndex)
                   from len in Enumerable.Range(6, Math.Min(5, lastMatchingIndex - i))

                   from dateWithSeprRegex in dateWithSeparatorRegexes
                   let dateMatch = dateWithSeprRegex.Match(password, i, len)
                   where dateMatch.Success

                   // Match the candidate date that likely takes the fewest guesses:
                   //  a date closest to the current date.
                   let date = SwapDayMonthByMetric(dateMatch.Groups["year"].Value.ToInt(),
                                                   dateMatch.Groups["month_or_day"].Value.ToInt(),
                                                   dateMatch.Groups["day_or_month"].Value.ToInt())
                   where IsDateInRange(date.year, date.month, date.day)
                   select new DateMatch
                   {
                       Pattern = Pattern.Date,
                       Token = dateMatch.Value,
                       i = i,
                       j = i + len - 1,
                       Separator = dateMatch.Groups["separator"].Value,
                       Year = date.year,
                       Month = date.month,
                       Day = date.day,
                       Entropy = CalculateEntropy(date.year, true)
                   };
        }//DelimitedDateMatch

        // True if the values of the day and the month require a swap (e.g. 30/11 -> 11/30); otherwise False.
        private static bool IsNeedSwapDayMonth(int day, int month) => (12 < month && day <= 12);

        private static int Metric(int candidateYear) => Math.Abs(calendar.ToFourDigitYear(candidateYear) - Guessing.ReferenceYear);

        private static int Metric(int year, int month, int day) =>
            IsNeedSwapDayMonth(day, month) ? int.MaxValue
                : (new DateTime(calendar.ToFourDigitYear(year), month, day) - Scoring.ReferenceDate).Duration().Days;

        // Takes the fewest guesses: a date closest to the current date.
        private static (int year, int month, int day) SwapDayMonthByMetric(int year, int month, int day) =>
           Metric(year, day, month) < Metric(year, month, day) ? (year, month: day, day: month) : (year, month, day);

        private static (int year, int month, int day) MapIntsToDate(int left, int middle, int right)
        {
            /*
             * Given a 3-tuple, discard if:
             *   "middle" is over 31 (for all date formats, years are never allowed in the middle)
             *   "middle" is zero
             *   Any int value is over the max allowable year
             *   Any int value over two digits but under the min allowable year
             *   2 int-values are over 31, the max allowable day
             *   2 int-values are zero
             *   All int-values are over 12, the max allowable month
             */
            if (middle <= 0 || middle > 31) return (0, 0, 0);

            int over12 = 0;
            int over31 = 0;
            int under1 = 0;
            foreach (int i in new int[] { left, middle, right })
            {
                if ((99 < i && i < DateMinYear) || i > DateMaxYear) return (0, 0, 0);
                if (i > 31) over31++;
                if (i > 12) over12++;
                if (i <= 0) under1++;
            }
            if (over31 >= 2 || over12 == 3 || under1 >= 2) return (0, 0, 0);

            // Match the candidate date that likely takes the fewest guesses:
            //  a date closest to the current date.
            (int year, int month, int day) = Metric(left) < Metric(right)   // (year first) < (year last)
                ? SwapDayMonthByMetric(left, middle, right)     // Year-Month-Day
                : SwapDayMonthByMetric(right, middle, left);    // Month-Day-Year

            return IsDateInRange(year, month, day) ? (calendar.ToFourDigitYear(year), month, day) : (0, 0, 0);
        }//MapIntsToDate

        /// <summary>
        /// Returns an indication of whether the specified
        /// <paramref name="year"/>, <paramref name="month"/>, and <paramref name="day"/> are valid.
        /// </summary>
        /// <param name="year">The year (1 through the number of years in the <see cref="DateTime.MaxValue"/> year).
        /// <para>A two-digit or four-digit year is allowed and
        /// internally evaluated as a four-digit year based on the Gregorian calendar.</para></param>
        /// <param name="month">The month (1 through the number of months in the Gregorian calendar).</param>
        /// <param name="day">The day (1 through the number of days in <paramref name="year"/> and <paramref name="month"/>).
        /// <para><seealso cref="DateTime.DaysInMonth"/></para></param>
        /// <returns><c>true</c> if the specified <paramref name="year"/>, <paramref name="month"/>, and <paramref name="day"/> are valid;
        /// otherwise, <c>false</c>.</returns>
        private static bool IsDateInRange(int year, int month, int day)
        {
            year = calendar.ToFourDigitYear(year);
            return (DateMinYear <= year && year <= DateMaxYear) && (1 <= month && month <= 12) &&
                   (1 <= day && day <= DateTime.DaysInMonth(year, month));
        }//IsDateInRange

        private static double CalculateEntropy(int year, bool separator)
        {
            // 100 years (two-digits) or the maximum year - 100 years (four-digit years valid range)
            double entropy = Math.Log(((year < 100) ? 100 : DateMaxYear - 100) * 12 * 31, 2);

            // Extra two bits for separator (/\...)
            return separator ? entropy + 2 : entropy;
        }//CalculateEntropy
    }

    /// <summary>
    /// A match made with the <see cref="DateMatcher"/> that contains some additional information specific to the date match.
    /// </summary>
    public class DateMatch : Match
    {
        /// <value>Where a date with separators is matched, this will contain the separator that was used (e.g. '/', '-')</value>
        public string Separator { get; set; }

        /// <value>The detected year</value>
        public int Year { get; set; }

        /// <value>The detected month</value>
        public int Month { get; set; }

        /// <value>The detected day</value>
        public int Day { get; set; }

        public override bool Equals(object obj) => obj is DateMatch match
            && base.Equals(match)
            && Separator == match.Separator
            && Year == match.Year
            && Month == match.Month
            && Day == match.Day;

        public override int GetHashCode()
        {
            var hashCode = 1558458600;
            hashCode = hashCode * -1521134295 + base.GetHashCode();
            hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(Separator);
            hashCode = hashCode * -1521134295 + Year.GetHashCode();
            hashCode = hashCode * -1521134295 + Month.GetHashCode();
            hashCode = hashCode * -1521134295 + Day.GetHashCode();
            return hashCode;
        }//GetHashCode
    }
}
