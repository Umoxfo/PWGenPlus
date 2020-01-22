using System;
using System.Globalization;

namespace Umoxfo.Zxcvbn.Report
{
    public static class TimeEstimates
    {
        private const decimal OneHandredPerHour = 100 / 3600m;
        private const decimal TenPerSecond = 10m;
        private const decimal OneE4PerSecond = 1e4m;
        private const decimal OneE10PerSecond = 1e10m;

        private const long Minute = 60;
        private const long Hour = Minute * 60;
        private const long Day = Hour * 24;
        private const long Month = (long)(Day * 30.436875); //The mean month length of the Gregorian calendar
        private const long Year = Day * 365;
        private const long Century = Year * 100;

        /// <summary>
        /// Calculate a rough estimate of crack time for entropy,
        /// see zxcbn scoring.coffee for more information on the model used
        /// </summary>
        /// <param name="entropy">Entropy of password</param>
        /// <returns>An estimation of seconds taken to crack password</returns>
        public static (CrackTimeSeconds CrackTimesSeconds, CrackTimeDisplay CrackTimesDisplay) EstimateAttackTimes(double guesses, in Translation translation = Translation.English)
        {
            decimal guessesM = double.IsInfinity(guesses) ? decimal.MaxValue : (decimal)guesses;

            var crackTimeSeconds = new CrackTimeSeconds(guessesM / OneHandredPerHour,
                                                        guessesM / TenPerSecond,
                                                        guessesM / OneE4PerSecond,
                                                        guessesM / OneE10PerSecond);

            var crackTimesDisplay = new CrackTimeDisplay(
                DisplayTime(crackTimeSeconds.OnlineThrottling100PerHour, translation),
                DisplayTime(crackTimeSeconds.OnlineNoThrottling10PerSecond, translation),
                DisplayTime(crackTimeSeconds.OfflineSlowHashing1e4PerSecond, translation),
                DisplayTime(crackTimeSeconds.OfflineFastHashing1e10PerSecond, translation));

            return (CrackTimesSeconds: crackTimeSeconds, CrackTimesDisplay: crackTimesDisplay);
        }//EstimateAttackTimes

        /// <summary>
        /// Converts a specified number of seconds to a human-friendly format, rounding up the decimal points.
        /// </summary>
        /// <param name="seconds">A number of seconds</param>
        /// <param name="translation">Language in which the string is returned</param>
        /// <returns>A human-friendly time string</returns>
        public static string DisplayTime(decimal seconds, in Translation translation = Translation.English)
        {
            CultureInfo culture = Utility.ToCultureInfo(translation);
            Properties.Resources.Culture = culture;

            if (seconds < 1) return Properties.Resources.Instant;
            else if (seconds < Minute)
                return $"{decimal.Round(seconds, MidpointRounding.AwayFromZero).ToString("N0", culture)} {Properties.Resources.Seconds}";
            else if (seconds < Hour)
                return $"{Divide(seconds, Minute).ToString("N0", culture)} {Properties.Resources.Minutes}";
            else if (seconds < Day)
                return $"{Divide(seconds, Hour).ToString("N0", culture)} {Properties.Resources.Hours}";
            else if (seconds < Month)
                return $"{Divide(seconds, Day).ToString("N0", culture)} {Properties.Resources.Days}";
            else if (seconds < Year)
                return $"{Divide(seconds, Month).ToString("N0", culture)} {Properties.Resources.Months}";
            else if (seconds < Century)
                return $"{Divide(seconds, Year).ToString("N0", culture)} {Properties.Resources.Years}";
            else if (seconds < Century)
                return $"{Divide(seconds, Year).ToString("N0", culture)} {Properties.Resources.Years}";
            else return $"{Divide(seconds, Century).ToString("N0", culture)} {Properties.Resources.Centuries}";
        }//DisplayTime

        private static decimal Divide(decimal dividend, long divisor) => Math.Round(dividend / divisor, MidpointRounding.AwayFromZero);
    }//TimeEstimates

    /// <summary>
    /// Estimated crack time, in seconds
    /// </summary>
    public class CrackTimeSeconds
    {
        /// <summary>
        /// Online attacks on a service that rate limits password auth attempts.
        /// </summary>
        /// <value>Online attacks on a service that rate limits password auth attempts.</value>
        public decimal OnlineThrottling100PerHour { get; }

        /// <summary>
        /// Online attack on a service that doesn't rate limit,
        /// or where an attacker has outsmarted rate limiting.
        /// </summary>
        /// <value>Online attack on a service that doesn't rate limit,
        /// or where an attacker has outsmarted rate limiting.</value>
        public decimal OnlineNoThrottling10PerSecond { get; }

        /// <summary>
        /// Offline attack assumes multiple attackers,
        /// proper user-unique salting, and a slow hash function
        /// with moderate work factors, such as BCrypt, SCrypt, PBKDF2.
        /// </summary>
        /// <value>Offline attack assumes multiple attackers,
        /// proper user-unique salting, and a slow hash function
        /// with moderate work factors</value>
        public decimal OfflineSlowHashing1e4PerSecond { get; }

        /// <summary>
        /// Offline attack with user-unique salting but a fast hash function like
        /// SHA-1, SHA-256 or MD5. A wide range of reasonable numbers anywhere
        /// from one billion - one trillion guesses per second,
        /// depending on several cores and machines. Ball parking at 10B/sec.
        /// </summary>
        /// <value>Offline attack with user-unique salting but a fast hash function like
        /// SHA-1, SHA-256 or MD5.</value>
        public decimal OfflineFastHashing1e10PerSecond { get; }

        public CrackTimeSeconds(decimal onlineThrottling100PerHour,
                                decimal onlineNoThrottling10PerSecond,
                                decimal offlineSlowHashing1e4PerSecond,
                                decimal offlineFastHashing1e10PerSecond)
        {
            OnlineThrottling100PerHour = onlineThrottling100PerHour;
            OnlineNoThrottling10PerSecond = onlineNoThrottling10PerSecond;
            OfflineSlowHashing1e4PerSecond = offlineSlowHashing1e4PerSecond;
            OfflineFastHashing1e10PerSecond = offlineFastHashing1e10PerSecond;
        }
    }//CrackTimeSeconds

    /// <summary>
    /// The <see cref="CrackTimeSeconds">crack time</see>,
    /// as a human friendlier string:
    /// "instant", "3 minutes", "centuries", etc.
    /// </summary>
    /// <value>The <see cref="CrackTimeSeconds">crack time</see>,
    /// as a human friendlier string</value>
    public class CrackTimeDisplay
    {
        public string OnlineThrottling100PerHour { get; }
        public string OnlineNoThrottling10PerSecond { get; }
        public string OfflineSlowHashing1e4PerSecond { get; }
        public string OfflineFastHashing1e10PerSecond { get; }

        public CrackTimeDisplay(string onlineThrottling100PerHour,
                                string onlineNoThrottling10PerSecond,
                                string offlineSlowHashing1e4PerSecond,
                                string offlineFastHashing1e10PerSecond)
        {
            OnlineThrottling100PerHour = onlineThrottling100PerHour;
            OnlineNoThrottling10PerSecond = onlineNoThrottling10PerSecond;
            OfflineSlowHashing1e4PerSecond = offlineSlowHashing1e4PerSecond;
            OfflineFastHashing1e10PerSecond = offlineFastHashing1e10PerSecond;
        }
    }//CrackTimeDisplay
}
