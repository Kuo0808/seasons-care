using System;
using SeasonsCare.Api.Models.Enums;

namespace SeasonsCare.Api.DTOs.Expenses
{
    public class UpdateExpenseRequest
    {
        /// <summary>
        /// 必填。支出標題，最長 100 字。
        /// </summary>
        public string Title { get; set; } = string.Empty;

        /// <summary>
        /// 必填。支出金額，需大於 0。
        /// </summary>
        public decimal Amount { get; set; }

        /// <summary>
        /// 必填。支出分類，僅支援 medical、food、traffic、other。
        /// </summary>
        public string Category { get; set; } = string.Empty;

        /// <summary>
        /// 選填。備註，最長 500 字。
        /// </summary>
        public string? Notes { get; set; }

        /// <summary>
        /// 必填。支出日期，請使用 ISO 8601 格式。
        /// </summary>
        public DateTime ExpenseDate { get; set; }

        /// <summary>
        /// 選填。分帳狀態，支援 pending、settled、none；省略時預設為 none。
        /// </summary>
        public ExpenseSplitStatus SplitStatus { get; set; } = ExpenseSplitStatus.None;

        /// <summary>
        /// 必填。前一次查詢到的 updatedAt，用於樂觀鎖檢查。
        /// </summary>
        public DateTime? UpdatedAt { get; set; }
    }
}
