using System;
using System.Collections.Generic;
using SeasonsCare.Api.Models.Enums;

namespace SeasonsCare.Api.DTOs.Events
{
    /// <summary>
    /// 新增事件（FME-1）。單次事件傳 repeatPattern = none。
    /// </summary>
    public class CreateEventRequest
    {
        public string Title { get; set; } = string.Empty;

        public string? Description { get; set; }

        public DateTimeOffset StartsAt { get; set; }

        public int? DurationMinutes { get; set; }

        /// <summary>none / daily / weeklyDay / monthly。</summary>
        public EventRepeatPattern RepeatPattern { get; set; } = EventRepeatPattern.None;

        public int RepeatInterval { get; set; } = 1;

        /// <summary>週重複時指定的星期（0=Sunday ... 6=Saturday）。</summary>
        public List<DayOfWeek>? DaysOfWeek { get; set; }

        /// <summary>0=Never、1=OnDate、2=AfterOccurrences。</summary>
        public EventSeriesEndType EndType { get; set; } = EventSeriesEndType.Never;

        public DateTimeOffset? EndAt { get; set; }

        public int? OccurrenceCount { get; set; }

        public List<string>? Participants { get; set; }

        public bool IsImportant { get; set; }
    }
}
