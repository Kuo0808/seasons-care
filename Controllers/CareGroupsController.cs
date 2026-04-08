using System;
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
    /// 照護群組 API。
    /// 管理照護群組的建立、更新、以及成員加入機制。
    /// </summary>
    [Authorize]
    [ApiController]
    [Route("api/care-groups")]
    // [架構導覽] 展示層 (Presentation Layer) - Controller
    // 職責：負責路由定義 (Routing)、接收 HTTP 請求、驗證輸入資料 (透過 Filter) 並回傳統一格式結果。本身不處理實質的商業規則。
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
        [EndpointDescription("建立新的照護群組。前端需在 request body 提供 recipientName，其他欄位如 recipientGender、recipientBirthDate 可選填；成功後會回傳新群組資料，並自動把目前登入者加入為管理員。")]
        public async Task<IActionResult> CreateCareGroup([FromBody] CreateCareGroupRequest request)
        {
            // 步驟 1：解析請求上下文 (例如：取得目前登入使用者 ID)
            var currentUserId = _currentUserService.UserId;
            
            // 步驟 2：將請求參數轉交給 Service 層 (大腦) 處理實質的商業邏輯
            var result = await _careGroupService.CreateAsync(currentUserId, request);

            // 步驟 3：包裝為專案統一標準的 ApiResponse 並回傳對應的 HTTP 狀態碼
            var response = new ApiResponse<CareGroupResponse>(result, "照護群組建立成功", HttpContext.TraceIdentifier);
            return StatusCode(201, response);
        }

        [HttpGet]
        [ProducesResponseType(typeof(ApiResponse<System.Collections.Generic.IEnumerable<CareGroupResponse>>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [EndpointSummary("取得我可存取的照護群組列表")]
        [EndpointDescription("取得目前登入使用者可存取的照護群組列表。前端可用 query string 傳入 page、pageSize、sort；回傳資料中的每筆 id 可作為後續取得單筆、更新群組或查詢群組底下資料的 careGroupId。")]
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
        [EndpointSummary("依照群組 ID 取得單一照護群組")]
        [EndpointDescription("依照 path 參數 id 取得單一照護群組的詳細資料。前端通常會先呼叫照護群組列表 API，再把回傳資料中的 id 帶到這支 API；成功後會回傳群組基本資料與成員列表。")]
        public async Task<IActionResult> GetById(Guid id)
        {
            var currentUserId = _currentUserService.UserId;
            var group = await _careGroupService.GetByIdAsync(currentUserId, id);
            var response = new ApiResponse<CareGroupDetailResponse>(group, "取得照護群組成功", HttpContext.TraceIdentifier);
            return Ok(response);
        }

        [HttpPut("{id}")]
        [ProducesResponseType(typeof(ApiResponse<CareGroupResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        [EndpointSummary("更新照護群組")]
        [EndpointDescription("更新指定照護群組的基本資料。前端需在 path 帶入 id，並在 request body 提供 name、recipientName 及其他可編輯欄位；id 通常來自照護群組列表或單筆群組查詢結果。")]
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
        [EndpointDescription("把目前登入使用者加入指定照護群組。前端需在 path 帶入群組 id，通常來自照護群組列表或邀請流程；若群組有邀請碼，則需在 request body 提供 inviteCode。")]
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
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        [EndpointSummary("移除成員或自行退出群組")]
        [EndpointDescription("移除指定群組中的某位成員，或由使用者自己退出群組。前端需在 path 帶入群組 id 與要移除的 userId；群組 id 通常來自照護群組資料，userId 可來自群組成員列表。")]
        public async Task<IActionResult> RemoveMember(Guid id, Guid userId)
        {
            var currentUserId = _currentUserService.UserId;
            await _careGroupService.RemoveMemberAsync(currentUserId, id, userId);
            var response = new ApiResponse<object>(null, "成員移除成功", HttpContext.TraceIdentifier);
            return Ok(response);
        }
    }
}
