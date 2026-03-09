namespace SeasonsCare.Api.DTOs.Auth
{
    public class RegisterRequest
    {
        public string Email { get; set; }
        
        public string Password { get; set; }
        
        public string Username { get; set; }
    }
}
