using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SeasonsCare.Api.DTOs.Auth;
using SeasonsCare.Api.DTOs.Common;
using Microsoft.AspNetCore.Http;
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
        [EndpointSummary("更新個人資料")]
        [EndpointDescription("更新目前登入使用者的個人資料 (例如姓名、頭像)。常在註冊後的引導流程中使用。")]
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
        [EndpointDescription("紀錄使用者最後一次登入時查看的照護群組 ID，方便下次登入直接跳轉預設群組。")]
        public async Task<IActionResult> UpdateLastViewedCareGroup([FromBody] UpdateLastViewedCareGroupRequest request)
        {
            var currentUserId = _currentUserService.UserId;
            await _authService.UpdateLastViewedCareGroupAsync(currentUserId, request.CareGroupId);
            var response = new ApiResponse<object?>(null, "更新上次瀏覽群組成功", HttpContext.TraceIdentifier);

            return Ok(response);
        }
    }
}
