using System.Text.Json.Serialization;

namespace SeasonsCare.Api.DTOs.Auth
{
    public class CompleteProfileRequest
    {
        [JsonPropertyName("userName")]
        public string Username { get; set; } = string.Empty;

        public string AvatarKey { get; set; } = string.Empty;
    }
}
