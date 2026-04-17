using System;
using System.Collections.Generic;

namespace SeasonsCare.Api.DTOs.Events
{
    /// <summary>
    /// 事件實例項目（FME-2 回傳陣列元素）。
    /// 代表某個 EventSeries 在某個時點的展開結果，已套用單次覆寫。
    /// </summary>
    public class EventOccurrenceItem
    {
        /// <summary>Occurrence 實體 ID；若此實例尚未產生 override，可能為 null。</summary>
        public Guid? Id { get; set; }

        /// <summary>所屬事件系列 ID（對應 FME-3 / FME-4 / FME-5 / FME-6 的 {eventId}）。</summary>
        public Guid EventSeriesId { get; set; }

        public string Title { get; set; } = string.Empty;

        /// <summary>此次實例預定時間（套用 override 後的值）。</summary>
        public DateTimeOffset ScheduledAt { get; set; }

        public List<string> Participants { get; set; } = new();

        /// <summary>事件狀態：pending / completed / cancelled。</summary>
        public string Status { get; set; } = "pending";

        public bool IsImportant { get; set; }

        public string? Notes { get; set; }

        /// <summary>系列的重複規則：none / daily / weekly / monthly（方便前端直接顯示標籤）。</summary>
        public string RepeatPattern { get; set; } = "none";

        /// <summary>此實例是否已被單次覆寫（true 代表 Status/Title/時間等已不同於系列預設）。</summary>
        public bool HasOverrides { get; set; }
    }
}
