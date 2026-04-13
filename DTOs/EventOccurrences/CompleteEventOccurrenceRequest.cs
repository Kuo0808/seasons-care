using System;

namespace SeasonsCare.Api.DTOs.EventOccurrences
{
    public class CompleteEventOccurrenceRequest
    {
        /// <summary>
        /// 必填。事件系列 ID。
        /// </summary>
        public Guid EventSeriesId { get; set; }

        /// <summary>
        /// 必填。要完成的事件實例開始時間，請帶入 occurrences API 回傳的 scheduledStartAt。
        /// </summary>
        public DateTime ScheduledStartAt { get; set; }
    }
}
