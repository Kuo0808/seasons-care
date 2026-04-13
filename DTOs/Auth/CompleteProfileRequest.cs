using System.Text.Json.Serialization;

namespace SeasonsCare.Api.DTOs.Auth
{
    public class CompleteProfileRequest
    {
        /// <summary>
        /// 必填。使用者名稱，前端欄位名稱為 userName，最長 50 字。
        /// </summary>
        [JsonPropertyName("userName")]
        public string Username { get; set; } = string.Empty;

        /// <summary>
        /// 必填。頭像代碼，最長 50 字。
        /// </summary>
        public string AvatarKey { get; set; } = string.Empty;
    }
}
