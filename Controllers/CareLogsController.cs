using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SeasonsCare.Api.DTOs.Common;
using SeasonsCare.Api.DTOs.CareLogs;
using Microsoft.AspNetCore.Http;
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
        [ProducesResponseType(typeof(ApiResponse<IEnumerable<CareLogResponse>>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [EndpointSummary("取得照護日誌列表")]
        [EndpointDescription("取得指定照護群組下的紀錄列表，支援分頁參數。")]
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
        [ProducesResponseType(typeof(ApiResponse<CareLogResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [EndpointSummary("取得單筆照護日誌")]
        [EndpointDescription("依照日誌 ID 取得單筆照護紀錄，若該紀錄不屬於目前 careGroupId，應會回傳 403 / 404。")]
        public async Task<IActionResult> GetLogById(Guid careGroupId, Guid logId)
        {
            var currentUserId = _currentUserService.UserId;
            var result = await _careLogService.GetLogByIdAsync(currentUserId, careGroupId, logId);
            var response = new ApiResponse<CareLogResponse>(result, "取得照護日誌成功", HttpContext.TraceIdentifier);
            return Ok(response);
        }

        [HttpPost]
        [ProducesResponseType(typeof(ApiResponse<CareLogResponse>), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [EndpointSummary("建立照護日誌")]
        [EndpointDescription("建立一筆新的照護日誌紀錄。")]
        public async Task<IActionResult> CreateLog(Guid careGroupId, [FromBody] CreateCareLogRequest request)
        {
            var currentUserId = _currentUserService.UserId;
            var result = await _careLogService.CreateLogAsync(currentUserId, careGroupId, request);
            var response = new ApiResponse<CareLogResponse>(result, "建立照護日誌成功", HttpContext.TraceIdentifier);
            return StatusCode(201, response);
        }

        [HttpPut("{logId}")]
        [ProducesResponseType(typeof(ApiResponse<CareLogResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        [EndpointSummary("更新照護日誌")]
        [EndpointDescription("更新既有的照護日誌內容。前端必須帶入 updatedAt，後端會用於 optimistic concurrency 檢查。")]
        public async Task<IActionResult> UpdateLog(Guid careGroupId, Guid logId, [FromBody] UpdateCareLogRequest request)
        {
            var currentUserId = _currentUserService.UserId;
            var result = await _careLogService.UpdateLogAsync(currentUserId, careGroupId, logId, request);
            var response = new ApiResponse<CareLogResponse>(result, "更新照護日誌成功", HttpContext.TraceIdentifier);
            return Ok(response);
        }

        [HttpDelete("{logId}")]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [EndpointSummary("刪除照護日誌")]
        [EndpointDescription("刪除指定照護日誌。此操作為 soft delete，資料仍保留於資料庫中。")]
        public async Task<IActionResult> DeleteLog(Guid careGroupId, Guid logId)
        {
            var currentUserId = _currentUserService.UserId;
            await _careLogService.DeleteLogAsync(currentUserId, careGroupId, logId);
            var response = new ApiResponse<object>(null, "刪除照護日誌成功", HttpContext.TraceIdentifier);
            return Ok(response);
        }
    }
}
