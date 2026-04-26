using System;

namespace SeasonsCare.Api.DTOs.Expenses
{
    /// <summary>
    /// 確認一鍵分帳後的回傳資料。
    /// 前端拿到 SplitBatchId 後可放進通知 payload，群組成員點擊通知時帶回 GET split-preview?splitBatchId={id} 查整批分帳結果。
    /// </summary>
    public class SplitConfirmResponse
    {
        /// <summary>
        /// 此次確認分帳產生的批次 Id。
        /// </summary>
        public Guid SplitBatchId { get; set; }

        /// <summary>
        /// 此次結算的支出筆數。
        /// </summary>
        public int ExpenseCount { get; set; }

        /// <summary>
        /// 此次結算的總金額。
        /// </summary>
        public decimal TotalAmount { get; set; }
    }
}
