using System;
using System.Collections.Generic;
using SeasonsCare.Api.Models.Enums;

namespace SeasonsCare.Api.DTOs.Events
{
    /// <summary>
    /// 事件回應（FME-1 / FME-3 回傳用；代表一個事件系列的當前規則）。
    /// </summary>
    public class EventResponse
    {
        /// <summary>事件系列 ID（作為 FME-3 / FME-4 / FME-5 / FME-6 的 {eventId}）。</summary>
        public Guid Id { get; set; }

        public string Title { get; set; } = string.Empty;
        public string? Description { get; set; }
        public DateTimeOffset StartsAt { get; set; }
        public int? DurationMinutes { get; set; }
        public EventRepeatPattern RepeatPattern { get; set; }
        public int RepeatInterval { get; set; }
        public List<DayOfWeek> DaysOfWeek { get; set; } = new();
        public EventSeriesEndType EndType { get; set; }
        public DateTimeOffset? EndAt { get; set; }
        public int? OccurrenceCount { get; set; }
        public List<string> Participants { get; set; } = new();
        public bool IsImportant { get; set; }

        public Guid CareGroupId { get; set; }
        public DateTimeOffset CreatedAt { get; set; }
        public DateTimeOffset? UpdatedAt { get; set; }
        public string CreatedBy { get; set; } = string.Empty;
    }
}
