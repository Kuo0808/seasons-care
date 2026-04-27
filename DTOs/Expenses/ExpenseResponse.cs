using System;
using SeasonsCare.Api.Models.Enums;

namespace SeasonsCare.Api.DTOs.Expenses
{
    /// <summary>
    /// 支出紀錄回傳模型。
    /// </summary>
    public class ExpenseResponse
    {
        /// <summary>
        /// 支出紀錄 ID。
        /// </summary>
        public Guid Id { get; set; }

        /// <summary>
        /// 支出標題。
        /// </summary>
        public string Title { get; set; } = string.Empty;

        /// <summary>
        /// 支出金額。
        /// </summary>
        public decimal Amount { get; set; }

        /// <summary>
        /// 支出分類，目前支援 medical、food、traffic、other。
        /// </summary>
        public string Category { get; set; } = string.Empty;

        /// <summary>
        /// 備註；若未填寫則回傳空字串。
        /// </summary>
        public string Notes { get; set; } = string.Empty;

        /// <summary>
        /// 支出發生時間。
        /// </summary>
        public DateTimeOffset ExpenseDate { get; set; }

        /// <summary>
        /// 分帳狀態。
        /// </summary>
        public ExpenseSplitStatus SplitStatus { get; set; }

        /// <summary>
        /// 所屬照護群組 ID。
        /// </summary>
        public Guid CareGroupId { get; set; }

        /// <summary>
        /// 建立時間。
        /// </summary>
        public DateTimeOffset CreatedAt { get; set; }

        /// <summary>
        /// 最後更新時間。
        /// </summary>
        public DateTimeOffset UpdatedAt { get; set; }

        /// <summary>
        /// 建立者使用者 ID。
        /// </summary>
        public string CreatedBy { get; set; } = string.Empty;

        /// <summary>
        /// 此支出所屬的分帳批次 Id。
        /// 規則：
        ///   - SplitStatus = Settled → 此欄位有值，前端可帶去 GET /split-preview?splitBatchId={id} 查分帳結果。
        ///   - SplitStatus = Pending / None → 此欄位為 null。
        ///   - 舊資料（migration 之前已 Settled 的支出）也會是 null，無法回查。
        /// </summary>
        public Guid? SplitBatchId { get; set; }
    }
}
