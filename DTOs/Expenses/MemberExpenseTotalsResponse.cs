using System;
using System.Collections.Generic;

namespace SeasonsCare.Api.DTOs.Expenses
{
    /// <summary>
    /// 各成員累積花費總覽（對應前端「各成員累積花費」卡片）。
    /// 用來呈現照護群組內每位成員「目前作為付款人」累積支付的金額。
    /// </summary>
    public class MemberExpenseTotalsResponse
    {
        /// <summary>
        /// 本次查詢條件下，群組所有成員加總後的總金額。
        /// </summary>
        public decimal TotalAmount { get; set; }

        /// <summary>
        /// 群組中納入計算的成員人數（含未付款者）。
        /// </summary>
        public int MemberCount { get; set; }

        /// <summary>
        /// 各成員累積花費明細，回傳順序：金額由高到低，金額相同者依名稱排序。
        /// </summary>
        public List<MemberExpenseTotalItem> Members { get; set; } = new List<MemberExpenseTotalItem>();
    }

    /// <summary>
    /// 單一成員的累積花費資訊。
    /// </summary>
    public class MemberExpenseTotalItem
    {
        public Guid UserId { get; set; }

        /// <summary>
        /// 成員顯示名稱。
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// 頭像 Key / URL（沿用 User.AvatarKey）。前端需自行組合 CDN base path 顯示。
        /// </summary>
        public string? AvatarUrl { get; set; }

        /// <summary>
        /// 該成員作為付款人（CreatedBy）所累積支付的總金額。沒有付款紀錄時為 0。
        /// </summary>
        public decimal TotalAmount { get; set; }

        /// <summary>
        /// 該成員作為付款人的支出筆數。
        /// </summary>
        public int ExpenseCount { get; set; }
    }
}
