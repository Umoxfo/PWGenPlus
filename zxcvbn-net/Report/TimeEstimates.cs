using System;
using System.Collections.Generic;
using System.Text;

namespace Zxcvbn.Report
{
    public class TimeEstimates
    {
        private readonly struct TimeUnit
        {
            internal const long Minute = 60;
            internal const long Hour = Minute * 60;
            internal const long Day = Hour * 24;
            internal const long Month = Day * 31;
            internal const long Year = Day * 365;
            internal const long Century = Year * 100;
        }

        /// <summary>
        /// Calculate a rough estimate of crack time for entropy,
        /// see zxcbn scoring.coffee for more information on the model used
        /// </summary>
        /// <param name="entropy">Entropy of password</param>
        /// <returns>An estimation of seconds taken to crack password</returns>
        public static (CrackTimeSeconds CrackTimesSeconds, CrackTimeDisplay CrackTimesDisplay) EstimateAttackTimes(double guesses, in Translation translation = Translation.English)
        {
            var crackTimeSeconds = new CrackTimeSeconds(guesses / (100 / 3600), guesses / 10, guesses / 1e4, guesses / 1e10);

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
        public static string DisplayTime(double seconds, in Translation translation = Translation.English)
        {
            Utility.SetTranslation(translation);

            if (seconds < 1) return Properties.Resources.Instant;
            else if (seconds < TimeUnit.Minute) return $"{Math.Round(seconds)} {Properties.Resources.Seconds}";
            else if (seconds < TimeUnit.Hour) return $"{Divide(seconds, TimeUnit.Minute)} {Properties.Resources.Minutes}";
            else if (seconds < TimeUnit.Day) return $"{Divide(seconds, TimeUnit.Hour)} {Properties.Resources.Hours}";
            else if (seconds < TimeUnit.Month) return $"{Divide(seconds, TimeUnit.Day)} {Properties.Resources.Days}";
            else if (seconds < TimeUnit.Year) return $"{Divide(seconds, TimeUnit.Month)} {Properties.Resources.Months}";
            else if (seconds < TimeUnit.Century) return $"{Divide(seconds, TimeUnit.Year)} {Properties.Resources.Years}";
            else return $"{Divide(seconds, TimeUnit.Century)} {Properties.Resources.Centuries}";
        }//DisplayTime

        private static decimal Divide(double dividend, double divisor) => decimal.Round((decimal)dividend / (decimal)divisor);
    }

    public readonly struct CrackTimeSeconds
    {
        public double OnlineThrottling100PerHour { get; }
        public double OnlineNoThrottling10PerSecond { get; }
        public double OfflineSlowHashing1e4PerSecond { get; }
        public double OfflineFastHashing1e10PerSecond { get; }

        public CrackTimeSeconds(double onlineThrottling100PerHour,
                                double onlineNoThrottling10PerSecond,
                                double offlineSlowHashing1e4PerSecond,
                                double offlineFastHashing1e10PerSecond)
        {
            OnlineThrottling100PerHour = onlineThrottling100PerHour;
            OnlineNoThrottling10PerSecond = onlineNoThrottling10PerSecond;
            OfflineSlowHashing1e4PerSecond = offlineSlowHashing1e4PerSecond;
            OfflineFastHashing1e10PerSecond = offlineFastHashing1e10PerSecond;
        }
    }

    public readonly struct CrackTimeDisplay
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
    }
}
