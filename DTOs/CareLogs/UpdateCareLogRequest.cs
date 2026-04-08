using System;

namespace SeasonsCare.Api.DTOs.CareLogs
{
    /// <summary>
    /// 更新照護日誌的 request body。
    /// 欄位命名採前端事件模型。
    /// </summary>
    public class UpdateCareLogRequest
    {
        /// <summary>
        /// 日誌標題。前端欄位名稱為 title。
        /// </summary>
        public string Title { get; set; } = string.Empty;

        /// <summary>
        /// 日誌描述內容。前端欄位名稱為 description。
        /// </summary>
        public string? Description { get; set; }

        /// <summary>
        /// 事件開始時間。前端欄位名稱為 startsAt，請使用 ISO 8601 UTC 時間字串。
        /// </summary>
        public DateTime? StartsAt { get; set; }

        /// <summary>
        /// 重複規則。前端欄位名稱為 repeatPattern，例如 none、daily、weekly。
        /// </summary>
        public string? RepeatPattern { get; set; }

        /// <summary>
        /// 參與者清單。前端欄位名稱為 participants，內容必須為該群組成員的 userId 字串。
        /// </summary>
        public List<string>? Participants { get; set; }

        /// <summary>
        /// 日誌狀態。前端欄位名稱為 status，例如 scheduled、done、cancelled。
        /// </summary>
        public string? Status { get; set; }

        /// <summary>
        /// 是否為重要日誌。前端欄位名稱為 isImportant。
        /// </summary>
        public bool IsImportant { get; set; }

        /// <summary>
        /// 前一次查詢到的更新時間。前端欄位名稱為 updatedAt，用於樂觀鎖檢查。
        /// </summary>
        public DateTime? UpdatedAt { get; set; }
    }
}
