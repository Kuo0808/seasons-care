using System;

namespace SeasonsCare.Api.Config
{
    public static class TimeHelper
    {
        private static readonly TimeZoneInfo TaiwanTimeZone = ResolveTaiwanTimeZone();

        public static DateTime UtcNow => DateTime.UtcNow;

        public static DateTime TaiwanNow => TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, TaiwanTimeZone);

        // Compatibility alias. New code should use UtcNow or TaiwanNow explicitly.
        public static DateTime Now => UtcNow;

        public static DateTime ToTaiwanTime(DateTime utcDateTime)
        {
            var normalizedUtc = utcDateTime.Kind == DateTimeKind.Utc
                ? utcDateTime
                : utcDateTime.ToUniversalTime();

            return TimeZoneInfo.ConvertTimeFromUtc(normalizedUtc, TaiwanTimeZone);
        }

        public static DateTime? ToTaiwanTime(DateTime? utcDateTime)
        {
            return utcDateTime.HasValue ? ToTaiwanTime(utcDateTime.Value) : null;
        }

        public static DateTimeOffset ToTaiwanOffset(DateTime utcDateTime)
        {
            var taiwanTime = ToTaiwanTime(utcDateTime);
            return new DateTimeOffset(taiwanTime, TaiwanTimeZone.GetUtcOffset(taiwanTime));
        }

        public static DateTimeOffset? ToTaiwanOffset(DateTime? utcDateTime)
        {
            return utcDateTime.HasValue ? ToTaiwanOffset(utcDateTime.Value) : null;
        }

        public static DateTime GetTaiwanDateStartUtc(DateTime? utcDateTime = null)
        {
            var taiwanTime = ToTaiwanTime(utcDateTime ?? UtcNow);
            var taiwanDateStart = new DateTime(taiwanTime.Year, taiwanTime.Month, taiwanTime.Day, 0, 0, 0, DateTimeKind.Unspecified);
            return TimeZoneInfo.ConvertTimeToUtc(taiwanDateStart, TaiwanTimeZone);
        }

        private static TimeZoneInfo ResolveTaiwanTimeZone()
        {
            try
            {
                return TimeZoneInfo.FindSystemTimeZoneById("Asia/Taipei");
            }
            catch (TimeZoneNotFoundException)
            {
                return TimeZoneInfo.FindSystemTimeZoneById("Taipei Standard Time");
            }
            catch (InvalidTimeZoneException)
            {
                return TimeZoneInfo.FindSystemTimeZoneById("Taipei Standard Time");
            }
        }
    }
}
