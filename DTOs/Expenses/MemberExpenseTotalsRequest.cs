using System;

namespace SeasonsCare.Api.DTOs.Expenses
{
    /// <summary>
    /// 取得各成員累積花費的查詢條件。
    /// </summary>
    public class MemberExpenseTotalsRequest
    {
        /// <summary>
        /// 統計範圍：daily（指定日）、monthly（指定月）、all（全部）。預設 monthly。
        /// </summary>
        public string Scope { get; set; } = "monthly";

        /// <summary>
        /// 目標日期（台灣時區）。僅 daily / monthly 模式生效，all 會忽略。
        /// daily：抓該日；不傳預設今天。
        /// monthly：抓該日所屬月份；不傳預設本月。
        /// 範例：2026-04-15。
        /// </summary>
        public DateTime? TargetDate { get; set; }

        /// <summary>
        /// 是否只計算待分帳（Pending）的費用。預設 true，符合「一鍵分帳前的累積花費」用途。
        /// 若需顯示已結算或無需分帳的費用，傳 false 並搭配前端業務需求使用。
        /// </summary>
        public bool PendingOnly { get; set; } = true;
    }
}
