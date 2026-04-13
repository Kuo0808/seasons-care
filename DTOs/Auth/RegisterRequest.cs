namespace SeasonsCare.Api.DTOs.Auth
{
    public class RegisterRequest
    {
        /// <summary>
        /// 必填。登入 Email，需符合 Email 格式。
        /// </summary>
        public string Email { get; set; } = string.Empty;

        /// <summary>
        /// 必填。登入密碼，長度需介於 6 到 12 碼。
        /// </summary>
        public string Password { get; set; } = string.Empty;
    }
}
