using System;
using System.Collections.Generic;
using SeasonsCare.Api.Models.Enums;

namespace SeasonsCare.Api.DTOs.EventSeries
{
    /// <summary>
    /// 建立事件系列的 request body。
    /// </summary>
    public class CreateEventSeriesRequest
    {
        /// <summary>
        /// 系列標題。前端欄位名稱為 title。
        /// </summary>
        public string Title { get; set; } = string.Empty;

        /// <summary>
        /// 系列描述。前端欄位名稱為 description。
        /// </summary>
        public string? Description { get; set; }

        /// <summary>
        /// 系列起始時間。前端欄位名稱為 startsAt。
        /// </summary>
        public DateTime StartsAt { get; set; }

        /// <summary>
        /// 單次事件時長，單位為分鐘。前端欄位名稱為 durationMinutes。
        /// </summary>
        public int? DurationMinutes { get; set; }

        /// <summary>
        /// 重複規則。前端欄位名稱為 repeatPattern，目前支援 None、Weekly。
        /// </summary>
        // 前端可傳 none、daily、weeklyDay、monthly；weeklyDay 會映射為 Weekly。
        public EventRepeatPattern RepeatPattern { get; set; } = EventRepeatPattern.None;

        /// <summary>
        /// 重複間隔。前端欄位名稱為 repeatInterval，例如 1 代表每週，2 代表每兩週。
        /// </summary>
        public int RepeatInterval { get; set; } = 1;

        /// <summary>
        /// 星期清單。前端欄位名稱為 daysOfWeek，內容請填 Sunday、Monday、Tuesday、Wednesday、Thursday、Friday、Saturday。
        /// </summary>
        public List<DayOfWeek>? DaysOfWeek { get; set; }

        /// <summary>
        /// 系列結束條件。前端欄位名稱為 endType。
        /// </summary>
        public EventSeriesEndType EndType { get; set; } = EventSeriesEndType.Never;

        /// <summary>
        /// 系列結束日期。前端欄位名稱為 endAt。當 endType=OnDate 時使用。
        /// </summary>
        public DateTime? EndAt { get; set; }

        /// <summary>
        /// 系列總次數。前端欄位名稱為 occurrenceCount。當 endType=AfterOccurrences 時使用。
        /// </summary>
        public int? OccurrenceCount { get; set; }

        /// <summary>
        /// 陪同成員清單。前端欄位名稱為 participants，內容必須為該群組成員的 userId 字串。
        /// </summary>
        public List<string>? Participants { get; set; }

        /// <summary>
        /// 系列狀態。前端欄位名稱為 status，例如 scheduled、active、paused。
        /// </summary>
        public string? Status { get; set; }

        /// <summary>
        /// 是否為重要事件。前端欄位名稱為 isImportant。
        /// </summary>
        public bool IsImportant { get; set; }
    }
}
