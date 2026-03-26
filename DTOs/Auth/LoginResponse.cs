using System;

namespace SeasonsCare.Api.DTOs.Auth
{
    public class LoginResponse
    {
        public string Token { get; set; } = string.Empty;
        public UserDto User { get; set; } = null!;
    }

    public class UserDto
    {
        public Guid Id { get; set; }
        public string Email { get; set; } = string.Empty;
        public string Username { get; set; } = string.Empty;
        public string AvatarKey { get; set; } = string.Empty;
        public bool IsProfileCompleted { get; set; }
    }
}
