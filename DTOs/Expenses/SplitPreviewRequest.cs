using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace SeasonsCare.Api.DTOs.Expenses
{
    /// <summary>
    /// 一鍵分帳預覽請求模型
    /// </summary>
    public class SplitPreviewRequest
    {
        /// <summary>
        /// 要分帳的支出項目 ID 列表
        /// </summary>
        [Required]
        [MinLength(1, ErrorMessage = "至少需要選擇一筆支出項目")]
        public List<Guid> ExpenseIds { get; set; } = new List<Guid>();

        /// <summary>
        /// 參與分攤的使用者 ID 列表
        /// </summary>
        [Required]
        [MinLength(1, ErrorMessage = "至少需要選擇一位分攤對象")]
        public List<Guid> TargetUserIds { get; set; } = new List<Guid>();
    }
}
