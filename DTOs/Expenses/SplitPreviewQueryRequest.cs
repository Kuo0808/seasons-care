using System;
using System.Collections.Generic;

namespace SeasonsCare.Api.DTOs.Expenses
{
    /// <summary>
    /// GET 分帳預覽 API 的 query 參數。
    /// 同時支援兩種情境：
    ///   1. 分帳前試算：傳 SplitMode（+ TargetDate / ExpenseIds），回傳目前 Pending 支出的試算結果。
    ///   2. 已分帳結果查詢（給通知彈窗用）：傳 SplitBatchId，回傳該批次的歷史分帳結果。
    /// 兩個情境互斥，傳 SplitBatchId 時其他參數都會被忽略。
    /// </summary>
    public class SplitPreviewQueryRequest
    {
        /// <summary>
        /// 分帳模式：daily（當日分帳）、monthly（當月分帳）、custom（自選項目）。
        /// 未傳入時預設為 daily。
        /// custom 模式必須同時提供 ExpenseIds。
        /// 傳 SplitBatchId 時此參數會被忽略。
        /// </summary>
        public string SplitMode { get; set; } = "daily";

        /// <summary>
        /// 自選分帳模式時要納入預覽的支出 ID 清單。
        /// 僅在 SplitMode = custom 時使用；daily / monthly 模式下會忽略。
        /// Query string 傳法：?expenseIds=guid1&amp;expenseIds=guid2。
        /// </summary>
        public List<Guid>? ExpenseIds { get; set; }

        /// <summary>
        /// 目標日期（台灣時區，ISO 日期字串，例如 2026-04-15）。
        /// 僅 daily / monthly 模式生效，custom 模式會忽略。
        /// daily：抓取該日一整天的待分帳費用；不傳則預設今天。
        /// monthly：抓取該日所屬月份的待分帳費用（傳該月任一日皆可）；不傳則預設本月。
        /// </summary>
        public DateTime? TargetDate { get; set; }

        /// <summary>
        /// 已分帳結果查詢用：傳入 POST /split 回傳的 SplitBatchId，
        /// API 會改從歷史 ExpenseSplit 表組裝該批次的分帳結果（試算邏輯不會被觸發）。
        /// </summary>
        public Guid? SplitBatchId { get; set; }
    }
}
