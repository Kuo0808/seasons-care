using System;

namespace SeasonsCare.Api.DTOs.CareLogs
{
    public class UpdateCareLogRequest
    {
        /// <summary>
        /// 必填。照護日誌標題，最長 100 字。
        /// </summary>
        public string Title { get; set; } = string.Empty;

        /// <summary>
        /// 選填。照護日誌描述。
        /// </summary>
        public string? Description { get; set; }

        /// <summary>
        /// 選填。開始時間，請使用 ISO 8601 UTC 格式。
        /// </summary>
        public DateTime? StartsAt { get; set; }

        /// <summary>
        /// 選填。重複規則，最長 50 字。
        /// </summary>
        public string? RepeatPattern { get; set; }

        /// <summary>
        /// 選填。參與者 userId 清單，所有值都必須屬於該照護群組成員。
        /// </summary>
        public List<string>? Participants { get; set; }

        /// <summary>
        /// 選填。狀態，最長 50 字。
        /// </summary>
        public string? Status { get; set; }

        /// <summary>
        /// 選填。是否標記為重要；省略時預設為 false。
        /// </summary>
        public bool IsImportant { get; set; }

        /// <summary>
        /// 必填。前一次查詢到的 updatedAt，用於樂觀鎖檢查。
        /// </summary>
        public DateTime? UpdatedAt { get; set; }
    }
}
