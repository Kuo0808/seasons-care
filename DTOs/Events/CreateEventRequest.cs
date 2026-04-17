using System;
using System.Collections.Generic;

namespace SeasonsCare.Api.DTOs.Events
{
    /// <summary>
    /// 新增事件（FME-1）。
    /// 設計稿對應欄位：任務名稱 / 時間 / 是否標示為重複 / 參與者 / 是否重要 / 備註。
    /// 不重複的事件請傳 repeatPattern = "none"。
    /// </summary>
    public class CreateEventRequest
    {
        /// <summary>任務名稱。必填，最長 100 字。</summary>
        public string Title { get; set; } = string.Empty;

        /// <summary>事件發生時間（ISO 8601，含時區）。例：2026-01-12T10:00:00+08:00。</summary>
        public DateTime ScheduledAt { get; set; }

        /// <summary>重複規則：none / daily / weekly / monthly。</summary>
        public string RepeatPattern { get; set; } = "none";

        /// <summary>參與者 userId 清單。省略時視同只有建立者本人。</summary>
        public List<string>? Participants { get; set; }

        /// <summary>是否標記為重要；省略時為 false。</summary>
        public bool IsImportant { get; set; }

        /// <summary>備註；對應後端 EventSeries.Description。</summary>
        public string? Notes { get; set; }
    }
}
