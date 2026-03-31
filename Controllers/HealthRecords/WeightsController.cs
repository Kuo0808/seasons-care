using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using SeasonsCare.Api.DTOs.Common;
using SeasonsCare.Api.DTOs.HealthRecords.Weights;
using SeasonsCare.Api.Services;
using SeasonsCare.Api.Services.HealthRecords;

namespace SeasonsCare.Api.Controllers.HealthRecords
{
    [Authorize]
    [ApiController]
    [Route("api/care-groups/{careGroupId}/health-records/weights")]
    public class WeightsController : ControllerBase
    {
        private readonly IWeightService _weightService;
        private readonly ICurrentUserService _currentUserService;

        public WeightsController(IWeightService weightService, ICurrentUserService currentUserService)
        {
            _weightService = weightService;
            _currentUserService = currentUserService;
        }

        [HttpGet]
        [ProducesResponseType(typeof(ApiResponse<IEnumerable<WeightResponse>>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [EndpointSummary("取得體重紀錄列表")]
        [EndpointDescription("取得指定照護群組下的體重紀錄列表，支援分頁參數。")]
        public async Task<IActionResult> GetRecords(Guid careGroupId, [FromQuery] PaginationRequest request)
        {
            var currentUserId = _currentUserService.UserId;
            var pagedResult = await _weightService.GetRecordsAsync(currentUserId, careGroupId, request);
            
            var response = new ApiResponse<IEnumerable<WeightResponse>>(
                pagedResult.Items, 
                "取得體重紀錄列表成功", 
                HttpContext.TraceIdentifier, 
                pagedResult.Pagination);
            return Ok(response);
        }

        [HttpGet("{recordId}")]
        [ProducesResponseType(typeof(ApiResponse<WeightResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [EndpointSummary("取得單筆體重紀錄")]
        [EndpointDescription("依照 recordId 取得單筆體重紀錄，若該紀錄不屬於目前 careGroupId，應回傳 404。")]
        public async Task<IActionResult> GetRecordById(Guid careGroupId, Guid recordId)
        {
            var currentUserId = _currentUserService.UserId;
            var result = await _weightService.GetRecordByIdAsync(currentUserId, careGroupId, recordId);
            var response = new ApiResponse<WeightResponse>(result, "取得體重紀錄成功", HttpContext.TraceIdentifier);
            return Ok(response);
        }

        [HttpPost]
        [ProducesResponseType(typeof(ApiResponse<WeightResponse>), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [EndpointSummary("建立體重紀錄")]
        [EndpointDescription("建立一筆新的體重紀錄。")]
        public async Task<IActionResult> CreateRecord(Guid careGroupId, [FromBody] CreateWeightRequest request)
        {
            var currentUserId = _currentUserService.UserId;
            var result = await _weightService.CreateRecordAsync(currentUserId, careGroupId, request);
            var response = new ApiResponse<WeightResponse>(result, "建立體重紀錄成功", HttpContext.TraceIdentifier);
            return StatusCode(201, response);
        }

        [HttpPut("{recordId}")]
        [ProducesResponseType(typeof(ApiResponse<WeightResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        [EndpointSummary("更新體重紀錄")]
        [EndpointDescription("更新既有體重紀錄。前端必須帶入 updatedAt，後端會用於 optimistic concurrency 檢查。")]
        public async Task<IActionResult> UpdateRecord(Guid careGroupId, Guid recordId, [FromBody] UpdateWeightRequest request)
        {
            var currentUserId = _currentUserService.UserId;
            var result = await _weightService.UpdateRecordAsync(currentUserId, careGroupId, recordId, request);
            var response = new ApiResponse<WeightResponse>(result, "更新體重紀錄成功", HttpContext.TraceIdentifier);
            return Ok(response);
        }

        [HttpDelete("{recordId}")]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [EndpointSummary("刪除體重紀錄")]
        [EndpointDescription("刪除指定體重紀錄。此操作為 soft delete，資料仍保留於資料庫中。")]
        public async Task<IActionResult> DeleteRecord(Guid careGroupId, Guid recordId)
        {
            var currentUserId = _currentUserService.UserId;
            await _weightService.DeleteRecordAsync(currentUserId, careGroupId, recordId);
            var response = new ApiResponse<object>(null, "刪除體重紀錄成功", HttpContext.TraceIdentifier);
            return Ok(response);
        }
    }
}
