using System;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SeasonsCare.Api.DTOs.CareGroups;
using SeasonsCare.Api.DTOs.Common;
using Microsoft.AspNetCore.Http;
using SeasonsCare.Api.Services;
using System.IdentityModel.Tokens.Jwt;

namespace SeasonsCare.Api.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/care-groups")]
    public class CareGroupsController : ControllerBase
    {
        private readonly ICareGroupService _careGroupService;
        private readonly ICurrentUserService _currentUserService;

        public CareGroupsController(ICareGroupService careGroupService, ICurrentUserService currentUserService)
        {
            _careGroupService = careGroupService;
            _currentUserService = currentUserService;
        }

        [HttpPost]
        [ProducesResponseType(typeof(ApiResponse<CareGroupResponse>), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [EndpointSummary("建立照護群組")]
        [EndpointDescription("建立一個新的照護群組，並將建立者設為管理員 (Admin)。將會自動生成邀請碼 (InviteCode)。")]
        public async Task<IActionResult> CreateCareGroup([FromBody] CreateCareGroupRequest request)
        {
            var currentUserId = _currentUserService.UserId;
            var result = await _careGroupService.CreateAsync(currentUserId, request);
            
            var response = new ApiResponse<CareGroupResponse>(result, "建立照護群組成功", HttpContext.TraceIdentifier);
            // CreatedAtAction expects route values, here we just return StatusCode 201 with response
            return StatusCode(201, response);
        }

        [HttpGet]
        [ProducesResponseType(typeof(ApiResponse<System.Collections.Generic.IEnumerable<CareGroupResponse>>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [EndpointSummary("取得我的照護群組列表")]
        [EndpointDescription("取得目前登入使用者所參與的所有照護群組列表，支援分頁參數。")]
        public async Task<IActionResult> GetMyGroups([FromQuery] PaginationRequest paginationRequest)
        {
            var currentUserId = _currentUserService.UserId;
            var pagedResult = await _careGroupService.GetMyGroupsAsync(currentUserId, paginationRequest);
            var response = new ApiResponse<System.Collections.Generic.IEnumerable<CareGroupResponse>>(
                pagedResult.Items, 
                "取得照護群組列表成功", 
                HttpContext.TraceIdentifier, 
                pagedResult.Pagination);
            return Ok(response);
        }

        [HttpGet("{id}")]
        [ProducesResponseType(typeof(ApiResponse<CareGroupDetailResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [EndpointSummary("取得照護群組詳細資料")]
        [EndpointDescription("根據 ID 取得特定的照護群組詳細內容及其所有成員列表。")]
        public async Task<IActionResult> GetById(Guid id)
        {
            var currentUserId = _currentUserService.UserId;
            var group = await _careGroupService.GetByIdAsync(currentUserId, id);
            var response = new ApiResponse<CareGroupDetailResponse>(group, "取得照護群組詳細資料成功", HttpContext.TraceIdentifier);
            return Ok(response);
        }

        [HttpPut("{id}")]
        [ProducesResponseType(typeof(ApiResponse<CareGroupResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [EndpointSummary("更新照護群組")]
        [EndpointDescription("更新特定的照護群組與被照護者資訊。")]
        public async Task<IActionResult> UpdateCareGroup(Guid id, [FromBody] UpdateCareGroupRequest request)
        {
            var currentUserId = _currentUserService.UserId;
            var result = await _careGroupService.UpdateAsync(currentUserId, id, request);
            var response = new ApiResponse<CareGroupResponse>(result, "更新照護群組成功", HttpContext.TraceIdentifier);
            return Ok(response);
        }

        [HttpPost("{id}/members")]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        [EndpointSummary("加入照護群組")]
        [EndpointDescription("如果該群組有設定邀請碼，則可以透過輸入正確的邀請碼加入特定的照護群組。")]
        public async Task<IActionResult> JoinCareGroup(Guid id, [FromBody] JoinCareGroupRequest request)
        {
            var currentUserId = _currentUserService.UserId;
            await _careGroupService.JoinAsync(currentUserId, id, request);
            var response = new ApiResponse<object>(null, "加入照護群組成功", HttpContext.TraceIdentifier);
            return Ok(response);
        }

        [HttpDelete("{id}/members/{userId}")]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [EndpointSummary("移除成員或退出群組")]
        [EndpointDescription("如果是管理員，則可以移除其他成員；或成員本人可透過此 API 自行退出群組 (Soft Delete)。")]
        public async Task<IActionResult> RemoveMember(Guid id, Guid userId)
        {
            var currentUserId = _currentUserService.UserId;
            await _careGroupService.RemoveMemberAsync(currentUserId, id, userId);
            var response = new ApiResponse<object>(null, "移除成員或退出群組成功", HttpContext.TraceIdentifier);
            return Ok(response);
        }
    }
}
