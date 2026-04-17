using System;
using System.Collections.Generic;
using SeasonsCare.Api.Models.Enums;

namespace SeasonsCare.Api.DTOs.EventOccurrences
{
    /// <summary>
    /// 更新單次事件實例的請求。
    /// 以 eventSeriesId 與 scheduledStartAt 指定原始 occurrence，其他欄位則代表要覆寫的單次內容。
    /// </summary>
    public class UpdateEventOccurrenceRequest
    {
        /// <summary>
        /// 事件系列 ID。
        /// </summary>
        public Guid EventSeriesId { get; set; }

        /// <summary>
        /// 要更新的單次事件時間鍵。前端應傳目前畫面上看到的 scheduledStartAt。
        /// </summary>
        public DateTime ScheduledStartAt { get; set; }

        /// <summary>
        /// 單次覆寫標題。未提供則維持原值。
        /// </summary>
        public string? Title { get; set; }

        /// <summary>
        /// 單次覆寫描述。未提供則維持原值。
        /// </summary>
        public string? Description { get; set; }

        /// <summary>
        /// 單次覆寫開始時間。可用於改期。
        /// </summary>
        public DateTime? StartsAt { get; set; }

        /// <summary>
        /// 單次覆寫結束時間。若有提供且早於 startsAt 會驗證失敗。
        /// </summary>
        public DateTime? EndsAt { get; set; }

        /// <summary>
        /// 單次覆寫參與者 userId 清單。
        /// </summary>
        public List<string>? Participants { get; set; }

        /// <summary>
        /// 單次覆寫狀態，例如 pending、completed、cancelled。
        /// </summary>
        public EventOccurrenceStatus? Status { get; set; }

        /// <summary>
        /// 單次覆寫是否重要。
        /// </summary>
        public bool? IsImportant { get; set; }
    }
}
