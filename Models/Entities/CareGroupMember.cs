using System;
using SeasonsCare.Api.Models.Enums;

namespace SeasonsCare.Api.Models.Entities
{
    public class CareGroupMember
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public Guid CareGroupId { get; set; }
        public Guid UserId { get; set; }
        public CareGroupRole Role { get; set; } = CareGroupRole.Member;
        public DateTime JoinedAt { get; set; } = DateTime.UtcNow;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }
        public string CreatedBy { get; set; } = string.Empty;
        public DateTime? DeletedAt { get; set; }

        public CareGroup CareGroup { get; set; } = null!;
        public User User { get; set; } = null!;
    }
}
