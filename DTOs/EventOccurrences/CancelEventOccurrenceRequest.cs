using System;

namespace SeasonsCare.Api.DTOs.EventOccurrences
{
    /// <summary>
    /// 取消單次事件實例的 request body。
    /// </summary>
    public class CancelEventOccurrenceRequest
    {
        /// <summary>
        /// 所屬事件系列 ID。前端欄位名稱為 eventSeriesId。
        /// </summary>
        public Guid EventSeriesId { get; set; }

        /// <summary>
        /// 要取消的實例開始時間。前端欄位名稱為 scheduledStartAt，值應來自事件實例查詢 API 回傳結果。
        /// </summary>
        public DateTime ScheduledStartAt { get; set; }
    }
}
