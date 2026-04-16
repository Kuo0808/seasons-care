using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace SeasonsCare.Api.DTOs.Expenses
{
    /// <summary>
    /// 一鍵分帳預覽請求模型。
    /// 支援三種模式：daily（當日）、monthly（當月）、custom（自選項目）。
    /// </summary>
    public class SplitPreviewRequest
    {
        /// <summary>
        /// 分帳模式：daily（當日分帳）、monthly（當月分帳）、custom（自選項目）。
        /// daily / monthly 時後端自動撈取該區間內未結算的費用，ExpenseIds 可不傳。
        /// custom 時必須傳入 ExpenseIds。
        /// </summary>
        [Required(ErrorMessage = "請指定分帳模式")]
        public string SplitMode { get; set; } = "custom";

        /// <summary>
        /// 自選模式時，指定要分帳的支出 ID 清單。
        /// daily / monthly 模式下可不傳，後端會自動依日期區間撈取。
        /// </summary>
        public List<Guid>? ExpenseIds { get; set; }

        /// <summary>
        /// 參與分攤的使用者 ID 列表。
        /// </summary>
        [Required]
        [MinLength(1, ErrorMessage = "至少需要選擇一位分攤對象")]
        public List<Guid> TargetUserIds { get; set; } = new List<Guid>();
    }
}
