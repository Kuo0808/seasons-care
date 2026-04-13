using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SeasonsCare.Api.Config;
using SeasonsCare.Api.Models.Enums;

namespace SeasonsCare.Api.Models.Entities
{
    public class ExpenseRecord : IMultiTenantEntity, ISoftDeleteEntity
    {
        public Guid Id { get; set; } = Guid.NewGuid();

        [Required]
        [MaxLength(100)]
        public string Title { get; set; } = string.Empty;

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal Amount { get; set; }

        [Required]
        [MaxLength(50)]
        public string Category { get; set; } = string.Empty;

        public string? Notes { get; set; }

        public DateTime ExpenseDate { get; set; } = TimeHelper.Now;

        [Required]
        public ExpenseSplitStatus SplitStatus { get; set; } = ExpenseSplitStatus.None;

        [Required]
        public Guid CareGroupId { get; set; }

        [ForeignKey("CareGroupId")]
        public CareGroup CareGroup { get; set; } = null!;

        public DateTime CreatedAt { get; set; } = TimeHelper.Now;

        [ConcurrencyCheck]
        public DateTime? UpdatedAt { get; set; }

        public string CreatedBy { get; set; } = string.Empty;

        public DateTime? DeletedAt { get; set; }
    }
}
