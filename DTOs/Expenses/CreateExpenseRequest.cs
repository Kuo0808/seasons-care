using System;
using SeasonsCare.Api.Models.Enums;

namespace SeasonsCare.Api.DTOs.Expenses
{
    /// <summary>
    /// 建立支出紀錄的 request body。
    /// </summary>
    public class CreateExpenseRequest
    {
        /// <summary>
        /// 支出標題，必填，最多 100 字。
        /// </summary>
        public string Title { get; set; } = string.Empty;

        /// <summary>
        /// 支出金額，必須大於 0。
        /// </summary>
        public decimal Amount { get; set; }

        /// <summary>
        /// 支出分類，必填，目前支援 medical、food、traffic、other。
        /// </summary>
        public string Category { get; set; } = string.Empty;

        /// <summary>
        /// 備註，選填，最多 500 字。
        /// </summary>
        public string? Notes { get; set; }

        /// <summary>
        /// 支出發生時間，必填，請使用 ISO 8601 日期時間格式。
        /// </summary>
        public DateTime ExpenseDate { get; set; }

        /// <summary>
        /// 分帳狀態，預設為 none，目前支援 pending、settled、none。
        /// </summary>
        public ExpenseSplitStatus SplitStatus { get; set; } = ExpenseSplitStatus.None;
    }
}
