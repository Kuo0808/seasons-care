using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using SeasonsCare.Api.DTOs.Common;
using SeasonsCare.Api.DTOs.HealthRecords.BloodSugars;
using SeasonsCare.Api.Services;
using SeasonsCare.Api.Services.HealthRecords;

namespace SeasonsCare.Api.Controllers.HealthRecords
{
    // [架構導覽] 展示層 (Presentation Layer) - Controller
    // 職責：負責路由定義 (Routing)、接收 HTTP 請求、驗證輸入資料 (透過 Filter) 並回傳統一格式結果。本身不處理實質的商業規則。
    /// <summary>
    /// 血糖紀錄 API。
    /// 管理各照護群組下的血糖測量資料。
    /// </summary>
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
        [EndpointDescription("取得指定照護群組底下的血糖紀錄列表。前端需在 path 帶入 careGroupId，通常來自照護群組列表或目前選取中的群組；可另外用 query string 傳入 page、pageSize、sort。")]
        public async Task<IActionResult> GetRecords(Guid careGroupId, [FromQuery] DateRangePaginationRequest request)
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
        [EndpointSummary("依照 recordId 取得單筆血糖紀錄")]
        [EndpointDescription("依照 path 參數 careGroupId 與 recordId 取得單筆血糖紀錄。前端通常會先呼叫血糖列表 API，再把回傳資料中的 id 當作 recordId 帶入；若該資料不屬於目前 careGroupId，會回傳 404。")]
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
        [EndpointDescription("在指定照護群組底下建立新的血糖紀錄。前端需在 path 帶入 careGroupId，並在 request body 提供 glucoseLevel 與 measurementContext；notes 與 recordDate 可依需求填寫。")]
        public async Task<IActionResult> CreateRecord(Guid careGroupId, [FromBody] CreateBloodSugarRequest request)
        {
            // 步驟 1：解析請求上下文 (取得目前登入使用者 ID)
            var currentUserId = _currentUserService.UserId;
            // 步驟 2：將請求參數轉交給 Service 層 (大腦) 處理實質的商業邏輯
            var result = await _bloodSugarService.CreateRecordAsync(currentUserId, careGroupId, request);
            // 步驟 3：包裝為專案統一標準的 ApiResponse 並回傳對應的 HTTP 狀態碼
            var response = new ApiResponse<BloodSugarResponse>(result, "建立血糖紀錄成功", HttpContext.TraceIdentifier);
            return StatusCode(201, response);
        }

        [HttpPut("{recordId}")]
        [ProducesResponseType(typeof(ApiResponse<BloodSugarResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        [EndpointSummary("更新血糖紀錄")]
        [EndpointDescription("更新指定的血糖紀錄。前端需在 path 帶入 careGroupId 與 recordId，並在 request body 提供要更新的欄位與 updatedAt；updatedAt 應來自先前查詢單筆或列表 API 回傳的資料，用於樂觀鎖檢查。")]
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
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        [EndpointSummary("刪除血糖紀錄")]
        [EndpointDescription("刪除指定的血糖紀錄。前端需在 path 帶入 careGroupId 與 recordId；這兩個值通常來自血糖列表或單筆查詢結果。此操作為 soft delete，不會物理刪除資料。")]
        public async Task<IActionResult> DeleteRecord(Guid careGroupId, Guid recordId)
        {
            var currentUserId = _currentUserService.UserId;
            await _bloodSugarService.DeleteRecordAsync(currentUserId, careGroupId, recordId);
            var response = new ApiResponse<object>(null, "刪除血糖紀錄成功", HttpContext.TraceIdentifier);
            return Ok(response);
        }
    }
}
