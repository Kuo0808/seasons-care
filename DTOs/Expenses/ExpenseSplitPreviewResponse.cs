using System;
using System.Collections.Generic;

namespace SeasonsCare.Api.DTOs.Expenses
{
    /// <summary>
    /// 一鍵分帳預覽的回應模型
    /// </summary>
    public class ExpenseSplitPreviewResponse
    {
        /// <summary>
        /// 本次分帳的總金額
        /// </summary>
        public decimal TotalAmount { get; set; }

        /// <summary>
        /// 被選中的分帳項目明細 (供前端在畫面上半部顯示)
        /// </summary>
        public List<ExpenseItemSummary> SelectedExpenses { get; set; } = new List<ExpenseItemSummary>();

        /// <summary>
        /// 各成員的分帳結果 (包含應收、應付金額與使用者基本資料)
        /// </summary>
        public List<SplitUserDetail> SplitDetails { get; set; } = new List<SplitUserDetail>();
    }

    /// <summary>
    /// 被選中的分帳項目摘要
    /// </summary>
    public class ExpenseItemSummary
    {
        public Guid Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public decimal Amount { get; set; }
    }

    /// <summary>
    /// 個別使用者的分帳結果明細
    /// </summary>
    public class SplitUserDetail
    {
        public Guid UserId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? AvatarUrl { get; set; }
        
        /// <summary>
        /// 標示該使用者是否為付款人 (或是其中之一)
        /// </summary>
        public bool IsPayer { get; set; }

        /// <summary>
        /// 應收金額 (若大於 0 代表別人該付給他)
        /// </summary>
        public decimal ReceivableAmount { get; set; }

        /// <summary>
        /// 應付金額 (若大於 0 代表他該付給別人)
        /// </summary>
        public decimal PayableAmount { get; set; }
    }
}
