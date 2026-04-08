using System;
using System.Collections.Generic;
using SeasonsCare.Api.Models.Enums;

namespace SeasonsCare.Api.DTOs.EventSeries
{
    /// <summary>
    /// 事件系列回應資料。
    /// </summary>
    public class EventSeriesResponse
    {
        /// <summary>
        /// 事件系列 ID。
        /// </summary>
        public Guid Id { get; set; }

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
        /// 重複規則。前端欄位名稱為 repeatPattern。
        /// </summary>
        public EventRepeatPattern RepeatPattern { get; set; }

        /// <summary>
        /// 重複間隔。前端欄位名稱為 repeatInterval。
        /// </summary>
        public int RepeatInterval { get; set; }

        /// <summary>
        /// 星期清單。前端欄位名稱為 daysOfWeek。
        /// </summary>
        public List<DayOfWeek> DaysOfWeek { get; set; } = new();

        /// <summary>
        /// 系列結束條件。前端欄位名稱為 endType。
        /// </summary>
        public EventSeriesEndType EndType { get; set; }

        /// <summary>
        /// 系列結束日期。前端欄位名稱為 endAt。
        /// </summary>
        public DateTime? EndAt { get; set; }

        /// <summary>
        /// 系列總次數。前端欄位名稱為 occurrenceCount。
        /// </summary>
        public int? OccurrenceCount { get; set; }

        /// <summary>
        /// 陪同成員清單。前端欄位名稱為 participants。
        /// </summary>
        public List<string> Participants { get; set; } = new();

        /// <summary>
        /// 系列狀態。前端欄位名稱為 status。
        /// </summary>
        public string? Status { get; set; }

        /// <summary>
        /// 是否為重要事件。前端欄位名稱為 isImportant。
        /// </summary>
        public bool IsImportant { get; set; }

        /// <summary>
        /// 所屬照護群組 ID。
        /// </summary>
        public Guid CareGroupId { get; set; }

        /// <summary>
        /// 建立時間。
        /// </summary>
        public DateTime CreatedAt { get; set; }

        /// <summary>
        /// 最後更新時間。
        /// </summary>
        public DateTime? UpdatedAt { get; set; }

        /// <summary>
        /// 建立者 userId。
        /// </summary>
        public string CreatedBy { get; set; } = string.Empty;
    }
}
