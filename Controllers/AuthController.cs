using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SeasonsCare.Api.DTOs.Auth;
using SeasonsCare.Api.DTOs.Common;
using SeasonsCare.Api.Exceptions;
using SeasonsCare.Api.Services;

namespace SeasonsCare.Api.Controllers
{
    [ApiController]
    [Route("api/auth")]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _authService;
        private readonly ICurrentUserService _currentUserService;

        public AuthController(IAuthService authService, ICurrentUserService currentUserService)
        {
            _authService = authService;
            _currentUserService = currentUserService;
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterRequest request)
        {
            var result = await _authService.RegisterAsync(request);
            var response = new ApiResponse<LoginResponse>(result, "註冊成功", HttpContext.TraceIdentifier);

            return StatusCode(201, response);
        }

        [Authorize]
        [HttpPost("complete-profile")]
        public async Task<IActionResult> CompleteProfile([FromBody] CompleteProfileRequest request)
        {
            var currentUserId = _currentUserService.UserId;
            var result = await _authService.CompleteProfileAsync(currentUserId, request);
            var response = new ApiResponse<LoginResponse>(result, "個人資料設定成功", HttpContext.TraceIdentifier);

            return Ok(response);
        }

        [HttpPost("login")]
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
