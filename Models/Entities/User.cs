using System;

namespace SeasonsCare.Api.Models.Entities
{
    public class User
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        
        public string Email { get; set; } = string.Empty;
        
        public string PasswordHash { get; set; } = string.Empty;
        
        public string Username { get; set; } = string.Empty;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        
        public DateTime? UpdatedAt { get; set; }
        
        public string CreatedBy { get; set; } = string.Empty;
        
        public DateTime? DeletedAt { get; set; }
    }
}
