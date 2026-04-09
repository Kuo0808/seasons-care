using System;

namespace SeasonsCare.Api.DTOs.EventOccurrences
{
    /// <summary>
    /// 標記單次事件已完成的 request body。
    /// </summary>
    public class CompleteEventOccurrenceRequest
    {
        /// <summary>
        /// 事件系列 ID。前端欄位名稱為 eventSeriesId。
        /// </summary>
        public Guid EventSeriesId { get; set; }

        /// <summary>
        /// 要標記完成的實例開始時間。值應直接使用事件實例查詢 API 回傳結果。
        /// </summary>
        public DateTime ScheduledStartAt { get; set; }
    }
}
