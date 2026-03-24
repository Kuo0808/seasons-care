using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using SeasonsCare.Api.DTOs.Common;
using SeasonsCare.Api.DTOs.HealthRecords.BloodSugars;
using SeasonsCare.Api.Services;
using SeasonsCare.Api.Services.HealthRecords;

namespace SeasonsCare.Api.Controllers.HealthRecords
{
    [Authorize]
    [ApiController]
    [Route("api/care-groups/{careGroupId}/health-records/blood-sugars")]
    public class BloodSugarsController : ControllerBase
    {
        private readonly IBloodSugarService _bloodSugarService;
        private readonly ICurrentUserService _currentUserService;

        public BloodSugarsController(IBloodSugarService bloodSugarService, ICurrentUserService currentUserService)
        {
            _bloodSugarService = bloodSugarService;
            _currentUserService = currentUserService;
        }

        [HttpGet]
        [ProducesResponseType(typeof(ApiResponse<IEnumerable<BloodSugarResponse>>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [EndpointSummary("取得血糖紀錄列表")]
        [EndpointDescription("取得指定照護群組下的血糖紀錄列表，支援分頁參數。")]
        public async Task<IActionResult> GetRecords(Guid careGroupId, [FromQuery] PaginationRequest request)
        {
            var currentUserId = _currentUserService.UserId;
            var pagedResult = await _bloodSugarService.GetRecordsAsync(currentUserId, careGroupId, request);
            
            var response = new ApiResponse<IEnumerable<BloodSugarResponse>>(
                pagedResult.Items, 
                "取得血糖紀錄列表成功", 
                HttpContext.TraceIdentifier, 
                pagedResult.Pagination);
            return Ok(response);
        }

        [HttpGet("{recordId}")]
        [ProducesResponseType(typeof(ApiResponse<BloodSugarResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [EndpointSummary("取得單筆血糖紀錄")]
        [EndpointDescription("依照 recordId 取得單筆血糖紀錄，若該紀錄不屬於目前 careGroupId，應回傳 404。")]
        public async Task<IActionResult> GetRecordById(Guid careGroupId, Guid recordId)
        {
            var currentUserId = _currentUserService.UserId;
            var result = await _bloodSugarService.GetRecordByIdAsync(currentUserId, careGroupId, recordId);
            var response = new ApiResponse<BloodSugarResponse>(result, "取得血糖紀錄成功", HttpContext.TraceIdentifier);
            return Ok(response);
        }

        [HttpPost]
        [ProducesResponseType(typeof(ApiResponse<BloodSugarResponse>), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [EndpointSummary("建立血糖紀錄")]
        [EndpointDescription("建立一筆新的血糖紀錄。measurementContext 建議使用固定值，例如：飯前、飯後、睡前。")]
        public async Task<IActionResult> CreateRecord(Guid careGroupId, [FromBody] CreateBloodSugarRequest request)
        {
            var currentUserId = _currentUserService.UserId;
            var result = await _bloodSugarService.CreateRecordAsync(currentUserId, careGroupId, request);
            var response = new ApiResponse<BloodSugarResponse>(result, "建立血糖紀錄成功", HttpContext.TraceIdentifier);
            return StatusCode(201, response);
        }

        [HttpPut("{recordId}")]
        [ProducesResponseType(typeof(ApiResponse<BloodSugarResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        [EndpointSummary("更新血糖紀錄")]
        [EndpointDescription("更新既有血糖紀錄。前端必須帶入 updatedAt，後端會用於 optimistic concurrency 檢查。")]
        public async Task<IActionResult> UpdateRecord(Guid careGroupId, Guid recordId, [FromBody] UpdateBloodSugarRequest request)
        {
            var currentUserId = _currentUserService.UserId;
            var result = await _bloodSugarService.UpdateRecordAsync(currentUserId, careGroupId, recordId, request);
            var response = new ApiResponse<BloodSugarResponse>(result, "更新血糖紀錄成功", HttpContext.TraceIdentifier);
            return Ok(response);
        }

        [HttpDelete("{recordId}")]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [EndpointSummary("刪除血糖紀錄")]
        [EndpointDescription("刪除指定血糖紀錄。此操作為 soft delete，資料仍保留於資料庫中。")]
        public async Task<IActionResult> DeleteRecord(Guid careGroupId, Guid recordId)
        {
            var currentUserId = _currentUserService.UserId;
            await _bloodSugarService.DeleteRecordAsync(currentUserId, careGroupId, recordId);
            var response = new ApiResponse<object>(null, "刪除血糖紀錄成功", HttpContext.TraceIdentifier);
            return Ok(response);
        }
    }
}
