using System;

namespace SeasonsCare.Api.DTOs.EventOccurrences
{
    /// <summary>
    /// 清除單次事件覆寫的請求。
    /// 清除後會回復成 EventSeries 預設展開的內容。
    /// </summary>
    public class ClearEventOccurrenceOverrideRequest
    {
        /// <summary>
        /// 事件系列 ID。
        /// </summary>
        public Guid EventSeriesId { get; set; }

        /// <summary>
        /// 要清除覆寫的單次事件時間鍵。前端應傳目前畫面上看到的 scheduledStartAt。
        /// </summary>
        public DateTime ScheduledStartAt { get; set; }
    }
}
