using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace SeasonsCare.Api.DTOs.Expenses
{
    /// <summary>
    /// 確認一鍵分帳的請求模型
    /// </summary>
    public class SplitConfirmRequest
    {
        /// <summary>
        /// 要結算分帳的支出項目 ID 列表
        /// </summary>
        [Required]
        [MinLength(1, ErrorMessage = "至少需要選擇一筆支出項目")]
        public List<Guid> ExpenseIds { get; set; } = new List<Guid>();

        /// <summary>
        /// 參與分攤的使用者 ID 列表 (用於供後端再次驗證或記錄)
        /// </summary>
        [Required]
        [MinLength(1, ErrorMessage = "至少需要選擇一位分攤對象")]
        public List<Guid> TargetUserIds { get; set; } = new List<Guid>();
    }
}
