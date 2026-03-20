using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SeasonsCare.Api.Models.Entities
{
    public class CareLog : IMultiTenantEntity, ISoftDeleteEntity
    {
        public Guid Id { get; set; } = Guid.NewGuid();

        [Required]
        [MaxLength(100)]
        public string Title { get; set; } = string.Empty;

        public string? Content { get; set; }

        public string? LogType { get; set; } // e.g., "Daily", "Medical", "Emergency", "Other"

        public DateTime RecordDate { get; set; } = DateTime.UtcNow;

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
