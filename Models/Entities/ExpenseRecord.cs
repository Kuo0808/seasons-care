using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

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

        [MaxLength(50)]
        public string? Category { get; set; } // e.g., "Medical", "Daily", "Transport", "Other"

        public string? Notes { get; set; }

        public DateTime ExpenseDate { get; set; } = DateTime.UtcNow;

        [Required]
        public Guid CareGroupId { get; set; }

        [ForeignKey("CareGroupId")]
        public CareGroup CareGroup { get; set; } = null!;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [ConcurrencyCheck]
        public DateTime? UpdatedAt { get; set; }

        public string CreatedBy { get; set; } = string.Empty;

        public DateTime? DeletedAt { get; set; }
    }
}
