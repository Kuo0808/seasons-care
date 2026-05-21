using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SeasonsCare.Api.Config;
using SeasonsCare.Api.Models.Enums;

namespace SeasonsCare.Api.Models.Entities
{
    public class Notification : IMultiTenantEntity, ISoftDeleteEntity
    {
        public Guid Id { get; set; } = Guid.NewGuid();

        [Required]
        public Guid CareGroupId { get; set; }

        [ForeignKey(nameof(CareGroupId))]
        public CareGroup CareGroup { get; set; } = null!;

        [Required]
        public Guid RecipientUserId { get; set; }

        [ForeignKey(nameof(RecipientUserId))]
        public User RecipientUser { get; set; } = null!;

        [Required]
        public NotificationType Type { get; set; }

        [Required]
        [MaxLength(100)]
        public string Title { get; set; } = string.Empty;

        [Required]
        [MaxLength(500)]
        public string Message { get; set; } = string.Empty;

        public string? PayloadJson { get; set; }

        [Required]
        public bool IsRead { get; set; }

        public DateTime? ReadAt { get; set; }

        public DateTime CreatedAt { get; set; } = TimeHelper.UtcNow;

        [ConcurrencyCheck]
        public DateTime? UpdatedAt { get; set; }

        public string CreatedBy { get; set; } = string.Empty;

        public DateTime? DeletedAt { get; set; }
    }
}
