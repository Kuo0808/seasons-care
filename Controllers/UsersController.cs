using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SeasonsCare.Api.DTOs.Auth;
using SeasonsCare.Api.DTOs.Common;
using SeasonsCare.Api.Services;

namespace SeasonsCare.Api.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/users")]
    public class UsersController : ControllerBase
    {
        private readonly IAuthService _authService;
        private readonly ICurrentUserService _currentUserService;

        public UsersController(IAuthService authService, ICurrentUserService currentUserService)
        {
            _authService = authService;
            _currentUserService = currentUserService;
        }

        [HttpPatch("me")]
        public async Task<IActionResult> UpdateMyProfile([FromBody] CompleteProfileRequest request)
        {
            var currentUserId = _currentUserService.UserId;
            var result = await _authService.CompleteProfileAsync(currentUserId, request);
            var response = new ApiResponse<LoginResponse>(result, "個人資料更新成功", HttpContext.TraceIdentifier);

            return Ok(response);
        }
    }
}
