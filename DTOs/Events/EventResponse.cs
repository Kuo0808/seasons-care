using System;
using System.Collections.Generic;
using SeasonsCare.Api.Models.Enums;

namespace SeasonsCare.Api.DTOs.Events
{
    /// <summary>
    /// 事件系列回應（FME-1 / FME-3）。
    /// </summary>
    public class EventResponse
    {
        public Guid Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string? Description { get; set; }
        public DateTimeOffset StartsAt { get; set; }
        public EventRepeatPattern RepeatPattern { get; set; }
        public List<DayOfWeek> DaysOfWeek { get; set; } = new();
        public List<string> Participants { get; set; } = new();
        public bool IsImportant { get; set; }

        public Guid CareGroupId { get; set; }
        public DateTimeOffset CreatedAt { get; set; }
        public DateTimeOffset? UpdatedAt { get; set; }
        public string CreatedBy { get; set; } = string.Empty;
    }
}
