using System;
using System.Collections.Generic;

namespace SeasonsCare.Api.Models.Entities
{
    public class CareGroup
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public string Name { get; set; } = string.Empty;
        public string RecipientName { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string? HealthStatus { get; set; }
        public string InviteCode { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }
        public string CreatedBy { get; set; } = string.Empty;
        public DateTime? DeletedAt { get; set; }

        public ICollection<CareGroupMember> Members { get; set; } = new List<CareGroupMember>();
    }
}
