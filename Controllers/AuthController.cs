using System.Linq;
using System.Threading.Tasks;
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

        public AuthController(IAuthService authService)
        {
            _authService = authService;
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterRequest request)
        {
            if (request == null)
            {
                throw new DomainException(
                    "請求內容為空，請檢查 Body",
                    "REQUEST_BODY_EMPTY",
                    400
                );
            }

            if (!ModelState.IsValid)
            {
                var errors = ModelState
                    .Where(x => x.Value.Errors.Count > 0)
                    .ToDictionary(
                        kvp => string.IsNullOrEmpty(kvp.Key) ? "body" : char.ToLowerInvariant(kvp.Key[0]) + kvp.Key.Substring(1),
                        kvp => kvp.Value.Errors.Select(e => e.ErrorMessage).ToArray()
                    );

                throw new DomainException(
                    "資料驗證失敗",
                    "VALIDATION_FAILED",
                    400,
                    errors
                );
            }

            await _authService.RegisterAsync(request);

            var response = new ApiResponse<object>(null, "註冊成功，請重新登入", HttpContext.TraceIdentifier);

            return StatusCode(201, response);
        }
    }
}
