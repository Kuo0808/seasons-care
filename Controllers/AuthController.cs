using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using SeasonsCare.Api.DTOs.Auth;
using SeasonsCare.Api.DTOs.Common;
using SeasonsCare.Api.Exceptions;
using SeasonsCare.Api.Services;
using Microsoft.AspNetCore.Http;

namespace SeasonsCare.Api.Controllers
{
    [ApiController]
    [Route("api/auth")]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _authService;

        public AuthController(IAuthService authService)
        {
            _authService = authService;
        }

        [HttpPost("register")]
        [ProducesResponseType(typeof(ApiResponse<LoginResponse>), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        [EndpointSummary("使用者註冊")]
        [EndpointDescription("建立新使用者帳號，成功註冊後會自動登入並回傳 JWT Token 與使用者資訊。")]
        public async Task<IActionResult> Register([FromBody] RegisterRequest request)
        {
            var result = await _authService.RegisterAsync(request);
            var response = new ApiResponse<LoginResponse>(result, "註冊成功", HttpContext.TraceIdentifier);

            return StatusCode(201, response);
        }

        [HttpPost("login")]
        [ProducesResponseType(typeof(ApiResponse<LoginResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [EndpointSummary("使用者登入")]
        [EndpointDescription("透過 Email 與密碼進行登入，登入成功會回傳 JWT Token 與使用者基本資訊。")]
        public async Task<IActionResult> Login([FromBody] LoginRequest request)
        {
            if (request == null || string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Password))
            {
                throw new DomainException(
                    "登入請求資料不完整",
                    "INVALID_LOGIN_REQUEST",
                    400
                );
            }

            var result = await _authService.LoginAsync(request);
            var response = new ApiResponse<LoginResponse>(result, "登入成功", HttpContext.TraceIdentifier);

            return Ok(response);
        }
    }
}
