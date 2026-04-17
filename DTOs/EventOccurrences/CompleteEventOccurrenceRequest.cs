using System;

namespace SeasonsCare.Api.DTOs.EventOccurrences
{
    /// <summary>
    /// 標記單次事件完成的請求。
    /// </summary>
    public class CompleteEventOccurrenceRequest
    {
        /// <summary>
        /// 事件系列 ID。
        /// </summary>
        public Guid EventSeriesId { get; set; }

        /// <summary>
        /// 要標記完成的單次事件時間鍵。前端應傳目前畫面上看到的 scheduledStartAt。
        /// </summary>
        public DateTime ScheduledStartAt { get; set; }
    }
}
