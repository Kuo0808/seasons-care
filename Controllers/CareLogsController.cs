using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SeasonsCare.Api.DTOs.Common;
using SeasonsCare.Api.DTOs.CareLogs;
using SeasonsCare.Api.Services;

namespace SeasonsCare.Api.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/care-groups/{careGroupId}/care-logs")]
    public class CareLogsController : ControllerBase
    {
        private readonly ICareLogService _careLogService;
        private readonly ICurrentUserService _currentUserService;

        public CareLogsController(ICareLogService careLogService, ICurrentUserService currentUserService)
        {
            _careLogService = careLogService;
            _currentUserService = currentUserService;
        }

        [HttpGet]
        public async Task<IActionResult> GetLogs(Guid careGroupId, [FromQuery] PaginationRequest paginationRequest)
        {
            var currentUserId = _currentUserService.UserId;
            var pagedResult = await _careLogService.GetLogsAsync(currentUserId, careGroupId, paginationRequest);
            var response = new ApiResponse<IEnumerable<CareLogResponse>>(
                pagedResult.Items, 
                "取得照護日誌列表成功", 
                HttpContext.TraceIdentifier, 
                pagedResult.Pagination);
            return Ok(response);
        }

        [HttpGet("{logId}")]
        public async Task<IActionResult> GetLogById(Guid careGroupId, Guid logId)
        {
            var currentUserId = _currentUserService.UserId;
            var result = await _careLogService.GetLogByIdAsync(currentUserId, careGroupId, logId);
            var response = new ApiResponse<CareLogResponse>(result, "取得照護日誌成功", HttpContext.TraceIdentifier);
            return Ok(response);
        }

        [HttpPost]
        public async Task<IActionResult> CreateLog(Guid careGroupId, [FromBody] CreateCareLogRequest request)
        {
            var currentUserId = _currentUserService.UserId;
            var result = await _careLogService.CreateLogAsync(currentUserId, careGroupId, request);
            var response = new ApiResponse<CareLogResponse>(result, "建立照護日誌成功", HttpContext.TraceIdentifier);
            return StatusCode(201, response);
        }

        [HttpPut("{logId}")]
        public async Task<IActionResult> UpdateLog(Guid careGroupId, Guid logId, [FromBody] UpdateCareLogRequest request)
        {
            var currentUserId = _currentUserService.UserId;
            var result = await _careLogService.UpdateLogAsync(currentUserId, careGroupId, logId, request);
            var response = new ApiResponse<CareLogResponse>(result, "更新照護日誌成功", HttpContext.TraceIdentifier);
            return Ok(response);
        }

        [HttpDelete("{logId}")]
        public async Task<IActionResult> DeleteLog(Guid careGroupId, Guid logId)
        {
            var currentUserId = _currentUserService.UserId;
            await _careLogService.DeleteLogAsync(currentUserId, careGroupId, logId);
            var response = new ApiResponse<object>(null, "刪除照護日誌成功", HttpContext.TraceIdentifier);
            return Ok(response);
        }
    }
}
