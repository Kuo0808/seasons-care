using System;
using System.Collections.Generic;

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
        public DateTime ScheduledAt { get; set; }
        public string RepeatPattern { get; set; } = "none";
        public List<string>? Participants { get; set; }
        public bool IsImportant { get; set; }
        public string? Notes { get; set; }
    }
}
