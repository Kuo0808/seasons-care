namespace SeasonsCare.Api.DTOs.Auth
{
    public class LoginRequest
    {
        /// <summary>
        /// 必填。登入 Email。
        /// </summary>
        public string Email { get; set; } = string.Empty;

        /// <summary>
        /// 必填。登入密碼。
        /// </summary>
        public string Password { get; set; } = string.Empty;
    }
}
