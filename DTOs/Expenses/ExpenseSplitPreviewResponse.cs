using System;
using System.Collections.Generic;

namespace SeasonsCare.Api.DTOs.Expenses
{
    /// <summary>
    /// 分帳預覽回傳資料。
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
