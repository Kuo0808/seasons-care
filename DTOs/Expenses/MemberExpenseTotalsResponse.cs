using System;
using System.Collections.Generic;

namespace SeasonsCare.Api.DTOs.Expenses
{
    /// <summary>
    /// 各成員累積花費總覽（對應前端「各成員累積花費」卡片）。
    /// 同時回傳兩種視角的金額，前端可直接依當前畫面需求取用，不需額外呼叫：
    /// - payer（待分帳）：依付款人（ExpenseRecord.CreatedBy）累積「尚未結算」的金額，對應一鍵分帳前的卡片。
    /// - share（已分攤）：依分攤對象（ExpenseSplit.UserId）累積已結算的 ShareAmount，對應分帳後的卡片。
    /// </summary>
    public class MemberExpenseTotalsResponse
    {
        /// <summary>
        /// 群組中納入計算的成員人數（含完全沒金額的成員）。
        /// </summary>
        public int MemberCount { get; set; }

        /// <summary>
        /// 「待分帳」付款人視角下所有成員加總後的總金額（僅含 Pending）。
        /// </summary>
        public decimal PayerTotalAmount { get; set; }

        /// <summary>
        /// 「已分攤」分攤對象視角下所有成員加總後的總金額（來自 ExpenseSplit）。
        /// </summary>
        public decimal ShareTotalAmount { get; set; }

        /// <summary>
        /// 全體成員「不分帳、由自己全額負擔」的支出總額。
        /// 來源為 ExpenseRecord，僅統計 splitStatus = none 且 createdBy = 該成員的金額。
        /// </summary>
        public decimal SelfExpenseTotalAmount { get; set; }

        /// <summary>
        /// 全體成員個人應支出總額。
        /// 計算方式為 shareTotalAmount + selfExpenseTotalAmount。
        /// </summary>
        public decimal PersonalPayableTotalAmount { get; set; }

        /// <summary>
        /// 全體成員目前即時總負擔金額。
        /// 計算方式為 payerTotalAmount + shareTotalAmount + selfExpenseTotalAmount。
        /// 可用來顯示「目前這個群組下，所有成員現在總共扛了多少金額」。
        /// </summary>
        public decimal CurrentPayableTotalAmount { get; set; }

        /// <summary>
        /// 各成員累積花費明細。每位成員同時包含兩個視角的金額。
        /// 排序：payerTotal + shareTotal 由高到低，相同者依名稱排序。
        /// </summary>
        public List<MemberExpenseTotalItem> Members { get; set; } = new List<MemberExpenseTotalItem>();
    }

    /// <summary>
    /// 單一成員的累積花費資訊（同時含付款人視角與分攤對象視角）。
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
        /// 付款人視角：該成員作為付款人（CreatedBy）「待分帳」的累積金額（僅含 Pending）。
        /// 分帳前的卡片取這個值。
        /// </summary>
        public decimal PayerTotal { get; set; }

        /// <summary>
        /// 付款人視角：該成員作為付款人、尚未結算的支出筆數。
        /// </summary>
        public int PayerCount { get; set; }

        /// <summary>
        /// 分攤對象視角：該成員作為分攤對象（ExpenseSplit.UserId）累積的 ShareAmount。
        /// 分帳後的卡片取這個值。
        /// </summary>
        public decimal ShareTotal { get; set; }

        /// <summary>
        /// 分攤對象視角：該成員出現在分帳明細中的筆數（即被分攤到幾筆 expense）。
        /// </summary>
        public int ShareCount { get; set; }

        /// <summary>
        /// 該成員自己建立、且不需要分帳（splitStatus = none）的支出總額。
        /// 這類支出不會出現在 shareTotal，而是由自己全額負擔。
        /// </summary>
        public decimal SelfExpenseTotal { get; set; }

        /// <summary>
        /// 該成員自己建立、且不需要分帳（splitStatus = none）的支出筆數。
        /// </summary>
        public int SelfExpenseCount { get; set; }

        /// <summary>
        /// 該成員個人應支出總額。
        /// 計算方式為 shareTotal + selfExpenseTotal。
        /// 例如：分帳後自己分到 1000，加上一筆不分帳 500，personalPayableTotal = 1500。
        /// </summary>
        public decimal PersonalPayableTotal { get; set; }

        /// <summary>
        /// 該成員目前即時總負擔金額。
        /// 計算方式為 payerTotal + shareTotal + selfExpenseTotal。
        /// 適合用來顯示「此人目前總共需要承擔多少」，包含待分帳代墊、已分帳應分、以及不分帳自付。
        /// </summary>
        public decimal CurrentPayableTotal { get; set; }
    }
}
