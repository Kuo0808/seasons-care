using System;
using System.Text.Json.Serialization;

namespace SeasonsCare.Api.DTOs.Auth
{
    public class LoginResponse
    {
        public string Token { get; set; } = string.Empty;
        public UserDto User { get; set; } = null!;
        public int CareGroupCount { get; set; }
        public Guid? DefaultCareGroupId { get; set; }
    }

    public class UserDto
    {
        public Guid Id { get; set; }
        public string Email { get; set; } = string.Empty;
        [JsonPropertyName("userName")]
        public string Username { get; set; } = string.Empty;
        public string AvatarKey { get; set; } = string.Empty;
        public bool IsProfileCompleted { get; set; }
    }
}
