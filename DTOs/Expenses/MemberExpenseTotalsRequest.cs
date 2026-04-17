using System;

namespace SeasonsCare.Api.DTOs.Expenses
{
    /// <summary>
    /// 取得各成員累積花費的查詢條件。
    /// 後端會同時回傳「待分帳（payer）」與「已分攤（share）」兩組數字，
    /// 前端不需指定視角，只要依畫面需求讀取對應欄位即可。
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
    }
}
