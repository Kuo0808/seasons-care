using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using SeasonsCare.Api.DTOs.Common;
using SeasonsCare.Api.DTOs.HealthRecords.Temperatures;
using SeasonsCare.Api.Services;
using SeasonsCare.Api.Services.HealthRecords;

namespace SeasonsCare.Api.Controllers.HealthRecords
{
    [Authorize]
    [ApiController]
    [Route("api/care-groups/{careGroupId}/health-records/temperatures")]
    public class TemperaturesController : ControllerBase
    {
        private readonly ITemperatureService _temperatureService;
        private readonly ICurrentUserService _currentUserService;

        public TemperaturesController(ITemperatureService temperatureService, ICurrentUserService currentUserService)
        {
            _temperatureService = temperatureService;
            _currentUserService = currentUserService;
        }

        [HttpGet]
        [ProducesResponseType(typeof(ApiResponse<IEnumerable<TemperatureResponse>>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [EndpointSummary("取得體溫紀錄列表")]
        [EndpointDescription("取得指定照護群組下的體溫紀錄列表，支援分頁參數。")]
        public async Task<IActionResult> GetRecords(Guid careGroupId, [FromQuery] PaginationRequest request)
        {
            var currentUserId = _currentUserService.UserId;
            var pagedResult = await _temperatureService.GetRecordsAsync(currentUserId, careGroupId, request);
            
            var response = new ApiResponse<IEnumerable<TemperatureResponse>>(
                pagedResult.Items, 
                "取得體溫紀錄列表成功", 
                HttpContext.TraceIdentifier, 
                pagedResult.Pagination);
            return Ok(response);
        }

        [HttpGet("{recordId}")]
        [ProducesResponseType(typeof(ApiResponse<TemperatureResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [EndpointSummary("取得單筆體溫紀錄")]
        [EndpointDescription("依照 recordId 取得單筆體溫紀錄，若該紀錄不屬於目前 careGroupId，應回傳 404。")]
        public async Task<IActionResult> GetRecordById(Guid careGroupId, Guid recordId)
        {
            var currentUserId = _currentUserService.UserId;
            var result = await _temperatureService.GetRecordByIdAsync(currentUserId, careGroupId, recordId);
            var response = new ApiResponse<TemperatureResponse>(result, "取得體溫紀錄成功", HttpContext.TraceIdentifier);
            return Ok(response);
        }

        [HttpPost]
        [ProducesResponseType(typeof(ApiResponse<TemperatureResponse>), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [EndpointSummary("建立體溫紀錄")]
        [EndpointDescription("建立一筆新的體溫紀錄。")]
        public async Task<IActionResult> CreateRecord(Guid careGroupId, [FromBody] CreateTemperatureRequest request)
        {
            var currentUserId = _currentUserService.UserId;
            var result = await _temperatureService.CreateRecordAsync(currentUserId, careGroupId, request);
            var response = new ApiResponse<TemperatureResponse>(result, "建立體溫紀錄成功", HttpContext.TraceIdentifier);
            return StatusCode(201, response);
        }

        [HttpPut("{recordId}")]
        [ProducesResponseType(typeof(ApiResponse<TemperatureResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        [EndpointSummary("更新體溫紀錄")]
        [EndpointDescription("更新既有體溫紀錄。前端必須帶入 updatedAt，後端會用於 optimistic concurrency 檢查。")]
        public async Task<IActionResult> UpdateRecord(Guid careGroupId, Guid recordId, [FromBody] UpdateTemperatureRequest request)
        {
            var currentUserId = _currentUserService.UserId;
            var result = await _temperatureService.UpdateRecordAsync(currentUserId, careGroupId, recordId, request);
            var response = new ApiResponse<TemperatureResponse>(result, "更新體溫紀錄成功", HttpContext.TraceIdentifier);
            return Ok(response);
        }

        [HttpDelete("{recordId}")]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [EndpointSummary("刪除體溫紀錄")]
        [EndpointDescription("刪除指定體溫紀錄。此操作為 soft delete，資料仍保留於資料庫中。")]
        public async Task<IActionResult> DeleteRecord(Guid careGroupId, Guid recordId)
        {
            var currentUserId = _currentUserService.UserId;
            await _temperatureService.DeleteRecordAsync(currentUserId, careGroupId, recordId);
            var response = new ApiResponse<object>(null, "刪除體溫紀錄成功", HttpContext.TraceIdentifier);
            return Ok(response);
        }
    }
}
