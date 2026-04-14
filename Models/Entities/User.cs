using System;
using System.ComponentModel.DataAnnotations;
using SeasonsCare.Api.Config;

namespace SeasonsCare.Api.Models.Entities
{
    public class User : ISoftDeleteEntity
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        
        public string Email { get; set; } = string.Empty;
        
        public string PasswordHash { get; set; } = string.Empty;
        
        public string Username { get; set; } = string.Empty;

        public string AvatarKey { get; set; } = string.Empty;

        public Guid? LastViewedCareGroupId { get; set; }

public DateTime CreatedAt { get; set; } = TimeHelper.UtcNow;
        
        [ConcurrencyCheck]
        public DateTime? UpdatedAt { get; set; }
        
        public string CreatedBy { get; set; } = string.Empty;
        
        public DateTime? DeletedAt { get; set; }
    }
}
