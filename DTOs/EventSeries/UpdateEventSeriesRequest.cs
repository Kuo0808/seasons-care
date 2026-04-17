using System;
using System.Collections.Generic;
using SeasonsCare.Api.Models.Enums;

namespace SeasonsCare.Api.DTOs.EventSeries
{
    /// <summary>
    /// 更新事件系列的請求。
    /// 所有欄位都會覆寫系列規則。未被單次覆寫（override）的 occurrence 會依新規則展開；
    /// 已被單次覆寫的 occurrence 則維持其原本覆寫內容。
    /// </summary>
    public class UpdateEventSeriesRequest
    {
        /// <summary>
        /// 必填。事件標題，最長 100 字。
        /// </summary>
        public string Title { get; set; } = string.Empty;

        /// <summary>
        /// 選填。事件描述。
        /// </summary>
        public string? Description { get; set; }

        /// <summary>
        /// 必填。事件開始時間，請使用 ISO 8601 UTC 格式。
        /// </summary>
        public DateTime StartsAt { get; set; }

        /// <summary>
        /// 選填。事件持續分鐘數，需大於 0。
        /// </summary>
        public int? DurationMinutes { get; set; }

        /// <summary>
        /// 選填。重複規則，預設為 none。
        /// </summary>
        public EventRepeatPattern RepeatPattern { get; set; } = EventRepeatPattern.None;

        /// <summary>
        /// 選填。重複間隔，預設為 1。
        /// </summary>
        public int RepeatInterval { get; set; } = 1;

        /// <summary>
        /// 選填。週期事件適用的星期清單。
        /// </summary>
        public List<DayOfWeek>? DaysOfWeek { get; set; }

        /// <summary>
        /// 選填。結束條件，預設為 never。
        /// </summary>
        public EventSeriesEndType EndType { get; set; } = EventSeriesEndType.Never;

        /// <summary>
        /// 選填。當 endType=OnDate 時使用的結束時間。
        /// </summary>
        public DateTime? EndAt { get; set; }

        /// <summary>
        /// 選填。當 endType=AfterOccurrences 時使用的發生次數。
        /// </summary>
        public int? OccurrenceCount { get; set; }

        /// <summary>
        /// 選填。參與者 userId 清單。
        /// </summary>
        public List<string>? Participants { get; set; }

        /// <summary>
        /// 選填。事件狀態。
        /// </summary>
        public string? Status { get; set; }

        /// <summary>
        /// 選填。是否標記為重要；省略時預設為 false。
        /// </summary>
        public bool IsImportant { get; set; }
    }
}
