using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace SeasonsCare.Api.Models.Entities
{
    public class CareGroup : ISoftDeleteEntity
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public string Name { get; set; } = string.Empty;
        public string RecipientName { get; set; } = string.Empty;
        public string? RecipientGender { get; set; }
        public DateOnly? RecipientBirthDate { get; set; }
        public string? Description { get; set; }
        public string? HealthStatus { get; set; }
        public string InviteCode { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [ConcurrencyCheck]
        public DateTime? UpdatedAt { get; set; }

        public string CreatedBy { get; set; } = string.Empty;
        public DateTime? DeletedAt { get; set; }

        public ICollection<CareGroupMember> Members { get; set; } = new List<CareGroupMember>();
    }
}
