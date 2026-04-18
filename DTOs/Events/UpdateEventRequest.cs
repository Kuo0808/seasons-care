using System;
using System.Collections.Generic;
using SeasonsCare.Api.Models.Enums;

namespace SeasonsCare.Api.DTOs.Events
{
    /// <summary>
    /// 編輯事件（FME-3）。覆寫整個系列；已被單次覆寫的 occurrence 不受影響。
    /// </summary>
    public class UpdateEventRequest
    {
        public string Title { get; set; } = string.Empty;
        public string? Description { get; set; }
        public DateTimeOffset StartsAt { get; set; }
        public EventRepeatPattern RepeatPattern { get; set; } = EventRepeatPattern.None;
        public List<DayOfWeek>? DaysOfWeek { get; set; }
        public List<string>? Participants { get; set; }
        public bool IsImportant { get; set; }
    }
}
