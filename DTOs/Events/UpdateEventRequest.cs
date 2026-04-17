using System;
using System.Collections.Generic;
using SeasonsCare.Api.Models.Enums;

namespace SeasonsCare.Api.DTOs.Events
{
    /// <summary>
    /// 編輯事件（FME-3）。
    /// 欄位與 CreateEventRequest 對齊；此請求會覆寫整個事件系列的規則與預設內容。
    /// 已被單次覆寫（override）的 occurrence 不受影響。
    /// </summary>
    public class UpdateEventRequest
    {
        public string Title { get; set; } = string.Empty;
        public string? Description { get; set; }
        public DateTime StartsAt { get; set; }
        public int? DurationMinutes { get; set; }
        public EventRepeatPattern RepeatPattern { get; set; } = EventRepeatPattern.None;
        public int RepeatInterval { get; set; } = 1;
        public List<DayOfWeek>? DaysOfWeek { get; set; }
        public EventSeriesEndType EndType { get; set; } = EventSeriesEndType.Never;
        public DateTime? EndAt { get; set; }
        public int? OccurrenceCount { get; set; }
        public List<string>? Participants { get; set; }
        public bool IsImportant { get; set; }
    }
}
