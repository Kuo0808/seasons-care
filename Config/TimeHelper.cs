using System;

namespace SeasonsCare.Api.Config
{
    /// <summary>
    /// 統一管理系統中的時間處理規則。
    /// 資料庫一律存 UTC，只有在畫面顯示或依台灣日界線切分資料時才轉為台灣時間。
    /// </summary>
    public static class TimeHelper
    {
        private static readonly TimeZoneInfo TaiwanTimeZone = ResolveTaiwanTimeZone();

        /// <summary>
        /// 給資料庫寫入、審計欄位與系統內部比較使用的目前 UTC 時間。
        /// </summary>
        public static DateTime UtcNow => DateTime.UtcNow;

        /// <summary>
        /// 給畫面顯示或依台灣在地時間判斷日期時使用的目前台灣時間。
        /// </summary>
        public static DateTime TaiwanNow => TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, TaiwanTimeZone);

        /// <summary>
        /// 舊有相容入口。為避免 timestamptz 寫入 Local/Unspecified DateTime，保留名稱但回 UTC。
        /// </summary>
        public static DateTime Now => UtcNow;

        public static DateTime ToTaiwanTime(DateTime utcDateTime)
        {
            var normalizedUtc = utcDateTime.Kind == DateTimeKind.Utc
                ? utcDateTime
                : utcDateTime.ToUniversalTime();

            return TimeZoneInfo.ConvertTimeFromUtc(normalizedUtc, TaiwanTimeZone);
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
