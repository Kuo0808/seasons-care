namespace SeasonsCare.Api.DTOs.Auth
{
    public class RegisterRequest
    {
        public string Email { get; set; } = string.Empty;
        
        public string Password { get; set; } = string.Empty;
        
        public string Username { get; set; } = string.Empty;

        public string AvatarKey { get; set; } = string.Empty;
    }
}
