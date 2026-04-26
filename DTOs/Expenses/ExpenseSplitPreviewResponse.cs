using System;
using System.Collections.Generic;

namespace SeasonsCare.Api.DTOs.Expenses
{
    /// <summary>
    /// 分帳預覽回傳資料。
    /// 同時支援兩種情境：
    ///   1. 分帳前試算（依 splitMode + targetDate 撈 Pending 支出）：ExecutedBy / ExecutedAt 為 null。
    ///   2. 已分帳結果回顧（依 splitBatchId 查歷史）：ExecutedBy / ExecutedAt 帶值，金額為當下結算結果。
    /// </summary>
    public class ExpenseSplitPreviewResponse
    {
        /// <summary>
        /// 本次預覽共包含幾筆帳目。
        /// </summary>
        public int ExpenseCount { get; set; }

        /// <summary>
        /// 本次預覽的總金額。
        /// </summary>
        public decimal TotalAmount { get; set; }

        /// <summary>
        /// 本次納入預覽的帳目明細。
        /// </summary>
        public List<ExpenseItemSummary> SelectedExpenses { get; set; } = new List<ExpenseItemSummary>();

        /// <summary>
        /// 每位成員的分帳預覽結果。
        /// </summary>
        public List<SplitUserDetail> SplitDetails { get; set; } = new List<SplitUserDetail>();

        /// <summary>
        /// 已分帳結果模式專用：執行此次分帳的人。試算模式為 null。
        /// </summary>
        public SplitExecutor? ExecutedBy { get; set; }

        /// <summary>
        /// 已分帳結果模式專用：分帳完成的時間（UTC）。試算模式為 null。
        /// </summary>
        public DateTime? ExecutedAt { get; set; }
    }

    /// <summary>
    /// 分帳執行者資訊。
    /// </summary>
    public class SplitExecutor
    {
        public Guid UserId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? AvatarUrl { get; set; }
    }

    /// <summary>
    /// 帳目摘要。
    /// </summary>
    public class ExpenseItemSummary
    {
        public Guid Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public decimal Amount { get; set; }
    }

    /// <summary>
    /// 成員分帳預覽結果。
    /// </summary>
    public class SplitUserDetail
    {
        public Guid UserId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? AvatarUrl { get; set; }
        public bool IsPayer { get; set; }
        public decimal ReceivableAmount { get; set; }
        public decimal PayableAmount { get; set; }
    }
}
