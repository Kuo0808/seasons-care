using System;
using System.Collections.Generic;
using SeasonsCare.Api.Models.Enums;

namespace SeasonsCare.Api.DTOs.Events
{
    /// <summary>
    /// 新增事件（FME-1）。
    /// 單次事件：repeatPattern = "none"；其餘欄位視使用情境填入。
    /// 週期事件：除必填外，可帶入 repeatInterval、daysOfWeek、endType、endAt、occurrenceCount 細節。
    /// </summary>
    public class CreateEventRequest
    {
        /// <summary>任務名稱。必填，最長 100 字。</summary>
        public string Title { get; set; } = string.Empty;

        /// <summary>備註／說明。</summary>
        public string? Description { get; set; }

        /// <summary>事件發生時間（ISO 8601，含時區）。</summary>
        public DateTime StartsAt { get; set; }

        /// <summary>事件持續分鐘數；省略代表無特定結束時間。</summary>
        public int? DurationMinutes { get; set; }

        /// <summary>重複規則：none / daily / weeklyDay / monthly。</summary>
        public EventRepeatPattern RepeatPattern { get; set; } = EventRepeatPattern.None;

        /// <summary>重複間隔；預設 1（每 1 日 / 1 週 / 1 月）。</summary>
        public int RepeatInterval { get; set; } = 1;

        /// <summary>週重複時指定的星期清單（0=Sunday, 1=Monday, ..., 6=Saturday）。</summary>
        public List<DayOfWeek>? DaysOfWeek { get; set; }

        /// <summary>結束條件：0=Never、1=OnDate、2=AfterOccurrences。</summary>
        public EventSeriesEndType EndType { get; set; } = EventSeriesEndType.Never;

        /// <summary>當 endType=OnDate 時使用的結束時間。</summary>
        public DateTime? EndAt { get; set; }

        /// <summary>當 endType=AfterOccurrences 時使用的總次數。</summary>
        public int? OccurrenceCount { get; set; }

        /// <summary>參與者 userId 清單。省略時視同只有建立者本人。</summary>
        public List<string>? Participants { get; set; }

        /// <summary>是否標記為重要；省略時為 false。</summary>
        public bool IsImportant { get; set; }
    }
}
