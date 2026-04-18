using System;
using System.Collections.Generic;
using SeasonsCare.Api.Models.Enums;

namespace SeasonsCare.Api.DTOs.Events
{
    /// <summary>
    /// 事件實例項目（FME-2 回傳陣列元素）。
    /// </summary>
    public class EventOccurrenceItem
    {
        /// <summary>對應的事件系列 Id（前端呼叫 FME-5 / FME-6 時使用）。</summary>
        public Guid EventSeriesId { get; set; }
        public string Title { get; set; } = string.Empty;
        public string? Description { get; set; }
        public DateTimeOffset ScheduledAt { get; set; }
        public List<string> Participants { get; set; } = new();

        /// <summary>pending / completed / cancelled。</summary>
        public string Status { get; set; } = "pending";

        public bool IsImportant { get; set; }
        public EventRepeatPattern RepeatPattern { get; set; }
    }
}
