using System;
using System.Collections.Generic;

namespace SeasonsCare.Api.DTOs.Events
{
    /// <summary>
    /// 事件回應（FME-1 / FME-3 回傳用；代表一個事件系列的當前規則）。
    /// </summary>
    public class EventResponse
    {
        /// <summary>事件系列 ID（作為 FME-3 / FME-4 的 {eventId}）。</summary>
        public Guid Id { get; set; }

        public string Title { get; set; } = string.Empty;
        public DateTimeOffset ScheduledAt { get; set; }
        public string RepeatPattern { get; set; } = "none";
        public List<string> Participants { get; set; } = new();
        public bool IsImportant { get; set; }
        public string? Notes { get; set; }

        public Guid CareGroupId { get; set; }
        public DateTimeOffset CreatedAt { get; set; }
        public DateTimeOffset? UpdatedAt { get; set; }
        public string CreatedBy { get; set; } = string.Empty;
    }
}
