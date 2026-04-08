using System;

namespace SeasonsCare.Api.DTOs.CareLogs
{
    /// <summary>
    /// 照護日誌回應資料。
    /// 欄位命名採前端事件模型。
    /// </summary>
    public class CareLogResponse
    {
        /// <summary>
        /// 日誌 ID。
        /// </summary>
        public Guid Id { get; set; }

        /// <summary>
        /// 日誌標題。前端欄位名稱為 title。
        /// </summary>
        public string Title { get; set; } = string.Empty;

        /// <summary>
        /// 日誌描述內容。前端欄位名稱為 description。
        /// </summary>
        public string? Description { get; set; }

        /// <summary>
        /// 事件開始時間。前端欄位名稱為 startsAt。
        /// </summary>
        public DateTime StartsAt { get; set; }

        /// <summary>
        /// 重複規則。前端欄位名稱為 repeatPattern。
        /// </summary>
        public string? RepeatPattern { get; set; }

        /// <summary>
        /// 參與者清單。前端欄位名稱為 participants，內容為群組成員 userId 字串。
        /// </summary>
        public List<string> Participants { get; set; } = new();

        /// <summary>
        /// 日誌狀態。前端欄位名稱為 status。
        /// </summary>
        public string? Status { get; set; }

        /// <summary>
        /// 是否為重要日誌。前端欄位名稱為 isImportant。
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
