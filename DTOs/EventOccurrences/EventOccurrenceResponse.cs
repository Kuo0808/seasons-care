using System;
using System.Collections.Generic;
using SeasonsCare.Api.Models.Enums;

namespace SeasonsCare.Api.DTOs.EventOccurrences
{
    /// <summary>
    /// 事件實例回應資料。
    /// 這是系列事件展開後的每一筆實際發生項目。
    /// </summary>
    public class EventOccurrenceResponse
    {
        /// <summary>
        /// 事件實例 ID。若該實例尚未被單次覆寫或取消，這個欄位可能為 null。
        /// </summary>
        public Guid? Id { get; set; }

        /// <summary>
        /// 所屬事件系列 ID。
        /// </summary>
        public Guid EventSeriesId { get; set; }

        /// <summary>
        /// 事件標題。
        /// </summary>
        public string Title { get; set; } = string.Empty;

        /// <summary>
        /// 事件描述。
        /// </summary>
        public string? Description { get; set; }

        /// <summary>
        /// 事件預定開始時間。
        /// </summary>
        public DateTime ScheduledStartAt { get; set; }

        /// <summary>
        /// 事件預定結束時間。
        /// </summary>
        public DateTime? ScheduledEndAt { get; set; }

        /// <summary>
        /// 參與者 userId 清單。
        /// </summary>
        public List<string> Participants { get; set; } = new();

        /// <summary>
        /// 事件狀態。
        /// </summary>
        public EventOccurrenceStatus Status { get; set; }

        /// <summary>
        /// 是否為重要事件。
        /// </summary>
        public bool IsImportant { get; set; }

        /// <summary>
        /// 是否來自單次覆寫資料。true 代表此筆實例已經被單次調整過。
        /// </summary>
        public bool HasOverrides { get; set; }
    }
}
