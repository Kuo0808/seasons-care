using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using SeasonsCare.Api.DTOs.Common;
using SeasonsCare.Api.DTOs.HealthRecords.BloodPressures;
using SeasonsCare.Api.Services;
using SeasonsCare.Api.Services.HealthRecords;

namespace SeasonsCare.Api.Controllers.HealthRecords
{
    [Authorize]
    [ApiController]
    [Route("api/care-groups/{careGroupId}/health-records/blood-pressures")]
    public class BloodPressuresController : ControllerBase
    {
        private readonly IBloodPressureService _bloodPressureService;
        private readonly ICurrentUserService _currentUserService;

        public BloodPressuresController(IBloodPressureService bloodPressureService, ICurrentUserService currentUserService)
        {
            _bloodPressureService = bloodPressureService;
            _currentUserService = currentUserService;
        }

        [HttpGet]
        [ProducesResponseType(typeof(ApiResponse<IEnumerable<BloodPressureResponse>>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [EndpointSummary("取得血壓紀錄列表")]
        [EndpointDescription("取得指定照護群組底下的血壓紀錄列表。前端需在 path 帶入 careGroupId，通常來自照護群組列表或目前選取中的群組；可另外用 query string 傳入 page、pageSize、sort。")]
        public async Task<IActionResult> GetRecords(Guid careGroupId, [FromQuery] PaginationRequest request)
        {
            var currentUserId = _currentUserService.UserId;
            var pagedResult = await _bloodPressureService.GetRecordsAsync(currentUserId, careGroupId, request);

            var response = new ApiResponse<IEnumerable<BloodPressureResponse>>(
                pagedResult.Items,
                "取得血壓紀錄列表成功",
                HttpContext.TraceIdentifier,
                pagedResult.Pagination);
            return Ok(response);
        }

        [HttpGet("{recordId}")]
        [ProducesResponseType(typeof(ApiResponse<BloodPressureResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [EndpointSummary("依照 recordId 取得單筆血壓紀錄")]
        [EndpointDescription("依照 path 參數 careGroupId 與 recordId 取得單筆血壓紀錄。前端通常會先呼叫血壓列表 API，再把回傳資料中的 id 當作 recordId 帶入；若該資料不屬於目前 careGroupId，會回傳 404。")]
        public async Task<IActionResult> GetRecordById(Guid careGroupId, Guid recordId)
        {
            var currentUserId = _currentUserService.UserId;
            var result = await _bloodPressureService.GetRecordByIdAsync(currentUserId, careGroupId, recordId);
            var response = new ApiResponse<BloodPressureResponse>(result, "取得血壓紀錄成功", HttpContext.TraceIdentifier);
            return Ok(response);
        }

        [HttpPost]
        [ProducesResponseType(typeof(ApiResponse<BloodPressureResponse>), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [EndpointSummary("建立血壓紀錄")]
        [EndpointDescription("在指定照護群組底下建立新的血壓紀錄。前端需在 path 帶入 careGroupId，並在 request body 提供 systolic 與 diastolic；notes 與 recordDate 可依需求填寫。")]
        public async Task<IActionResult> CreateRecord(Guid careGroupId, [FromBody] CreateBloodPressureRequest request)
        {
            var currentUserId = _currentUserService.UserId;
            var result = await _bloodPressureService.CreateRecordAsync(currentUserId, careGroupId, request);
            var response = new ApiResponse<BloodPressureResponse>(result, "建立血壓紀錄成功", HttpContext.TraceIdentifier);
            return StatusCode(201, response);
        }

        [HttpPut("{recordId}")]
        [ProducesResponseType(typeof(ApiResponse<BloodPressureResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        [EndpointSummary("更新血壓紀錄")]
        [EndpointDescription("更新指定的血壓紀錄。前端需在 path 帶入 careGroupId 與 recordId，並在 request body 提供要更新的欄位與 updatedAt；updatedAt 應來自先前查詢單筆或列表 API 回傳的資料，用於樂觀鎖檢查。")]
        public async Task<IActionResult> UpdateRecord(Guid careGroupId, Guid recordId, [FromBody] UpdateBloodPressureRequest request)
        {
            var currentUserId = _currentUserService.UserId;
            var result = await _bloodPressureService.UpdateRecordAsync(currentUserId, careGroupId, recordId, request);
            var response = new ApiResponse<BloodPressureResponse>(result, "更新血壓紀錄成功", HttpContext.TraceIdentifier);
            return Ok(response);
        }

        [HttpDelete("{recordId}")]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        [EndpointSummary("刪除血壓紀錄")]
        [EndpointDescription("刪除指定的血壓紀錄。前端需在 path 帶入 careGroupId 與 recordId；這兩個值通常來自血壓列表或單筆查詢結果。此操作為 soft delete，不會物理刪除資料。")]
        public async Task<IActionResult> DeleteRecord(Guid careGroupId, Guid recordId)
        {
            var currentUserId = _currentUserService.UserId;
            await _bloodPressureService.DeleteRecordAsync(currentUserId, careGroupId, recordId);
            var response = new ApiResponse<object>(null, "刪除血壓紀錄成功", HttpContext.TraceIdentifier);
            return Ok(response);
        }
    }
}
