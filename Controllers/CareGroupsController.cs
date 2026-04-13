using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using SeasonsCare.Api.DTOs.CareGroups;
using SeasonsCare.Api.DTOs.Common;
using SeasonsCare.Api.Services;

namespace SeasonsCare.Api.Controllers
{
    /// <summary>
    /// Care Group APIs.
    /// </summary>
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

        /// <summary>
        /// 建立照護群組。
        /// </summary>
        /// <param name="request">建立群組資料，需提供 recipientName、recipientGender、recipientBirthDate。</param>
        [HttpPost]
        [ProducesResponseType(typeof(ApiResponse<CareGroupResponse>), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [EndpointSummary("建立照護群組")]
        [EndpointDescription("建立新的照護群組，並自動把目前登入使用者加入成為管理者。成功後會回傳 careGroupId 與 inviteCode。")]
        public async Task<IActionResult> CreateCareGroup([FromBody] CreateCareGroupRequest request)
        {
            var currentUserId = _currentUserService.UserId;
            var result = await _careGroupService.CreateAsync(currentUserId, request);
            var response = new ApiResponse<CareGroupResponse>(result, "建立照護群組成功", HttpContext.TraceIdentifier);
            return StatusCode(201, response);
        }

        /// <summary>
        /// 取得目前使用者的照護群組列表。
        /// </summary>
        /// <param name="paginationRequest">分頁參數，可帶 page、pageSize、sort。</param>
        [HttpGet]
        [ProducesResponseType(typeof(ApiResponse<IEnumerable<CareGroupResponse>>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [EndpointSummary("取得我的照護群組列表")]
        [EndpointDescription("回傳目前登入使用者所屬的照護群組列表。支援 page、pageSize、sort 查詢參數。")]
        public async Task<IActionResult> GetMyGroups([FromQuery] PaginationRequest paginationRequest)
        {
            var currentUserId = _currentUserService.UserId;
            var pagedResult = await _careGroupService.GetMyGroupsAsync(currentUserId, paginationRequest);
            var response = new ApiResponse<IEnumerable<CareGroupResponse>>(
                pagedResult.Items,
                "取得照護群組列表成功",
                HttpContext.TraceIdentifier,
                pagedResult.Pagination);
            return Ok(response);
        }

        /// <summary>
        /// 取得單一照護群組詳情。
        /// </summary>
        /// <param name="id">照護群組 id。</param>
        [HttpGet("{id}")]
        [ProducesResponseType(typeof(ApiResponse<CareGroupDetailResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [EndpointSummary("取得單一照護群組")]
        [EndpointDescription("依照 path 參數 id 取得照護群組詳情。只有群組成員可以查看。")]
        public async Task<IActionResult> GetById(Guid id)
        {
            var currentUserId = _currentUserService.UserId;
            var group = await _careGroupService.GetByIdAsync(currentUserId, id);
            var response = new ApiResponse<CareGroupDetailResponse>(group, "取得照護群組成功", HttpContext.TraceIdentifier);
            return Ok(response);
        }

        /// <summary>
        /// 更新照護群組資料。
        /// </summary>
        /// <param name="id">照護群組 id。</param>
        /// <param name="request">更新資料。</param>
        [HttpPut("{id}")]
        [ProducesResponseType(typeof(ApiResponse<CareGroupResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        [EndpointSummary("更新照護群組")]
        [EndpointDescription("更新指定照護群組的基本資料。前端需在 path 帶入 id，並在 request body 提供要更新的欄位。")]
        public async Task<IActionResult> UpdateCareGroup(Guid id, [FromBody] UpdateCareGroupRequest request)
        {
            var currentUserId = _currentUserService.UserId;
            var result = await _careGroupService.UpdateAsync(currentUserId, id, request);
            var response = new ApiResponse<CareGroupResponse>(result, "更新照護群組成功", HttpContext.TraceIdentifier);
            return Ok(response);
        }

        /// <summary>
        /// 使用邀請碼加入照護群組。
        /// </summary>
        /// <param name="request">加入群組資料。前端只需提供 inviteCode。</param>
        [HttpPost("join")]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        [EndpointSummary("使用邀請碼加入照護群組")]
        [EndpointDescription("讓目前登入使用者只透過 inviteCode 加入照護群組。前端不需要先知道 groupId；只要在 request body 提供 inviteCode，後端會用 inviteCode 找到對應群組並完成加入流程。若 inviteCode 無效會回傳 401，若已經是群組成員則回傳 409。")]
        public async Task<IActionResult> JoinCareGroupByInviteCode([FromBody] JoinCareGroupRequest request)
        {
            var currentUserId = _currentUserService.UserId;
            await _careGroupService.JoinByInviteCodeAsync(currentUserId, request);
            var response = new ApiResponse<object>(null, "使用邀請碼加入照護群組成功", HttpContext.TraceIdentifier);
            return Ok(response);
        }

        /// <summary>
        /// 依群組 id 加入照護群組。
        /// </summary>
        /// <param name="id">照護群組 id。</param>
        /// <param name="request">加入群組資料，需提供 inviteCode。</param>
        [HttpPost("{id}/members")]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        [EndpointSummary("依群組 id 加入照護群組")]
        [EndpointDescription("把目前登入使用者加入指定照護群組。前端需在 path 帶入群組 id；若群組有邀請碼，則需在 request body 提供 inviteCode。這支 API 保留給已知 groupId 的流程使用。")]
        public async Task<IActionResult> JoinCareGroup(Guid id, [FromBody] JoinCareGroupRequest request)
        {
            var currentUserId = _currentUserService.UserId;
            await _careGroupService.JoinAsync(currentUserId, id, request);
            var response = new ApiResponse<object>(null, "加入照護群組成功", HttpContext.TraceIdentifier);
            return Ok(response);
        }

        /// <summary>
        /// 移除照護群組成員。
        /// </summary>
        /// <param name="id">照護群組 id。</param>
        /// <param name="userId">要移除的成員 userId。</param>
        [HttpDelete("{id}/members/{userId}")]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        [EndpointSummary("移除照護群組成員")]
        [EndpointDescription("從指定照護群組移除成員。只有成員自己或群組管理者可以執行。")]
        public async Task<IActionResult> RemoveMember(Guid id, Guid userId)
        {
            var currentUserId = _currentUserService.UserId;
            await _careGroupService.RemoveMemberAsync(currentUserId, id, userId);
            var response = new ApiResponse<object>(null, "移除照護群組成員成功", HttpContext.TraceIdentifier);
            return Ok(response);
        }
    }
}
