namespace SeasonsCare.Api.DTOs.Auth
{
    public class CompleteProfileRequest
    {
        public string Username { get; set; } = string.Empty;

        public string AvatarKey { get; set; } = string.Empty;
    }
}
