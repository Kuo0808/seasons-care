using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
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
        [ProducesResponseType(typeof(ApiResponse<LoginResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        [EndpointSummary("更新目前登入者的個人資料")]
        [EndpointDescription("更新目前登入使用者的個人資料。前端需在 request body 提供 username 與 avatarKey；成功後會回傳最新的使用者資訊與登入相關資料。")]
        public async Task<IActionResult> UpdateMyProfile([FromBody] CompleteProfileRequest request)
        {
            var currentUserId = _currentUserService.UserId;
            var result = await _authService.CompleteProfileAsync(currentUserId, request);
            var response = new ApiResponse<LoginResponse>(result, "個人資料更新成功", HttpContext.TraceIdentifier);

            return Ok(response);
        }

        [HttpPatch("me/last-viewed-care-group")]
        [ProducesResponseType(typeof(ApiResponse<object?>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        [EndpointSummary("更新最後查看的照護群組")]
        [EndpointDescription("記錄目前登入使用者最後一次查看的照護群組。前端需在 request body 提供 careGroupId，通常是使用者目前選取中的群組 ID；成功後不會回傳額外資料。")]
        public async Task<IActionResult> UpdateLastViewedCareGroup([FromBody] UpdateLastViewedCareGroupRequest request)
        {
            var currentUserId = _currentUserService.UserId;
            await _authService.UpdateLastViewedCareGroupAsync(currentUserId, request.CareGroupId);
            var response = new ApiResponse<object?>(null, "最後查看的照護群組已更新", HttpContext.TraceIdentifier);

            return Ok(response);
        }
    }
}
