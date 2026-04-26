using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SeasonsCare.Api.Config;

namespace SeasonsCare.Api.Models.Entities
{
    /// <summary>
    /// 分帳明細：記錄每一筆支出（ExpenseRecord）在「確認一鍵分帳」當下，分攤給每位成員的金額。
    /// 1 筆 ExpenseRecord 在結算時會展開為 N 筆 ExpenseSplit（N = 參與分攤的人數）。
    /// 使用情境：
    ///   - 結算後計算「各成員應分攤累積金額」（viewMode=share）
    ///   - 保留分帳歷史，未來可顯示「這筆費用怎麼分的」
    /// </summary>
    public class ExpenseSplit : IMultiTenantEntity, ISoftDeleteEntity
    {
        public Guid Id { get; set; } = Guid.NewGuid();

        /// <summary>
        /// 對應的支出紀錄 Id。
        /// </summary>
        [Required]
        public Guid ExpenseId { get; set; }

        [ForeignKey("ExpenseId")]
        public ExpenseRecord Expense { get; set; } = null!;

        /// <summary>
        /// 被分攤到的使用者 Id。
        /// </summary>
        [Required]
        public Guid UserId { get; set; }

        /// <summary>
        /// 該使用者在這筆支出中應分攤的金額。
        /// 平均分攤情況下為 expense.Amount / 分攤人數，四捨五入到小數第二位。
        /// </summary>
        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal ShareAmount { get; set; }

        /// <summary>
        /// 此 UserId 是否為原始付款人（即 ExpenseRecord.CreatedBy）。
        /// 用來在前端區分「他付過 / 他被分攤」。
        /// </summary>
        [Required]
        public bool IsPayer { get; set; }

        /// <summary>
        /// 多租戶隔離欄位（denormalize，方便不 join Expense 也能依 careGroup 查詢）。
        /// </summary>
        [Required]
        public Guid CareGroupId { get; set; }

        /// <summary>
        /// 分帳批次 Id：同一次「確認分帳」操作寫入的所有 split row 共用同一個 batchId。
        /// 用於前端通知點擊後查詢該批次的分帳結果。
        /// 舊資料（migration 之前產生）為 null。
        /// </summary>
        public Guid? SplitBatchId { get; set; }

        public DateTime CreatedAt { get; set; } = TimeHelper.UtcNow;

        public DateTime? UpdatedAt { get; set; }

        /// <summary>
        /// 建立此分帳列的使用者（通常為按下「確認分帳」的人）。
        /// </summary>
        public string CreatedBy { get; set; } = string.Empty;

        public DateTime? DeletedAt { get; set; }
    }
}
