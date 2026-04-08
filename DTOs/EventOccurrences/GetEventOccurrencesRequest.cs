using System;

namespace SeasonsCare.Api.DTOs.EventOccurrences
{
    /// <summary>
    /// 查詢事件實例區間的 query string。
    /// </summary>
    public class GetEventOccurrencesRequest
    {
        /// <summary>
        /// 查詢起始時間。前端欄位名稱為 from，請使用 ISO 8601 UTC 時間字串。
        /// </summary>
        public DateTime From { get; set; }

        /// <summary>
        /// 查詢結束時間。前端欄位名稱為 to，請使用 ISO 8601 UTC 時間字串。
        /// </summary>
        public DateTime To { get; set; }
    }
}
