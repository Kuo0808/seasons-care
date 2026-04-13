using System;

namespace SeasonsCare.Api.Config
{
    /// <summary>
    /// 統一的時間處理工具類別。
    /// 全專案取得「現在時間」時，應使用 TimeHelper.Now，而非 DateTime.UtcNow。
    /// </summary>
    public static class TimeHelper
    {
        private static readonly TimeZoneInfo TaiwanTimeZone =
            TimeZoneInfo.FindSystemTimeZoneById("Asia/Taipei");

        /// <summary>
        /// 取得當前台灣時間 (UTC+8)。
        /// </summary>
        public static DateTime Now =>
            TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, TaiwanTimeZone);
    }
}
