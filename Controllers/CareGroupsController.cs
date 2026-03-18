using System;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SeasonsCare.Api.DTOs.CareGroups;
using SeasonsCare.Api.DTOs.Common;
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
        public async Task<IActionResult> CreateCareGroup([FromBody] CreateCareGroupRequest request)
        {
            var currentUserId = _currentUserService.UserId;
            var result = await _careGroupService.CreateAsync(currentUserId, request);
            
            var response = new ApiResponse<CareGroupResponse>(result, "建立照護群組成功", HttpContext.TraceIdentifier);
            // CreatedAtAction expects route values, here we just return StatusCode 201 with response
            return StatusCode(201, response);
        }

        [HttpGet]
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
        public async Task<IActionResult> GetById(Guid id)
        {
            var currentUserId = _currentUserService.UserId;
            var group = await _careGroupService.GetByIdAsync(currentUserId, id);
            var response = new ApiResponse<CareGroupDetailResponse>(group, "取得照護群組詳細資料成功", HttpContext.TraceIdentifier);
            return Ok(response);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateCareGroup(Guid id, [FromBody] UpdateCareGroupRequest request)
        {
            var currentUserId = _currentUserService.UserId;
            var result = await _careGroupService.UpdateAsync(currentUserId, id, request);
            var response = new ApiResponse<CareGroupResponse>(result, "更新照護群組成功", HttpContext.TraceIdentifier);
            return Ok(response);
        }

        [HttpPost("{id}/members")]
        public async Task<IActionResult> JoinCareGroup(Guid id, [FromBody] JoinCareGroupRequest request)
        {
            var currentUserId = _currentUserService.UserId;
            await _careGroupService.JoinAsync(currentUserId, id, request);
            var response = new ApiResponse<object>(null, "加入照護群組成功", HttpContext.TraceIdentifier);
            return Ok(response);
        }

        [HttpDelete("{id}/members/{userId}")]
        public async Task<IActionResult> RemoveMember(Guid id, Guid userId)
        {
            var currentUserId = _currentUserService.UserId;
            await _careGroupService.RemoveMemberAsync(currentUserId, id, userId);
            var response = new ApiResponse<object>(null, "移除成員或退出群組成功", HttpContext.TraceIdentifier);
            return Ok(response);
        }
    }
}
