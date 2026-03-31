using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using SeasonsCare.Api.DTOs.CareLogs;
using SeasonsCare.Api.DTOs.Common;
using SeasonsCare.Api.Services;

namespace SeasonsCare.Api.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/care-groups/{careGroupId}/care-logs")]
    // [架構導覽] 展示層 (Presentation Layer) - Controller
    // 職責：負責路由定義 (Routing)、接收 HTTP 請求、驗證輸入資料 (透過 Filter)，並將請求轉派給 Service 層。Controller 本身不處理實質的商業規則。
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
        [EndpointDescription("取得指定照護群組底下的照護日誌列表。前端需在 path 帶入 careGroupId，通常來自照護群組列表或目前選取中的群組；可另外用 query string 傳入 page、pageSize、sort。")]
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
        [EndpointSummary("依照 logId 取得單筆照護日誌")]
        [EndpointDescription("依照 path 參數 careGroupId 與 logId 取得單筆照護日誌。前端通常會先呼叫照護日誌列表 API，再把回傳資料中的 id 當作 logId 帶入；若該資料不屬於目前 careGroupId，會回傳 404。")]
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
        [EndpointDescription("在指定照護群組底下建立新的照護日誌。前端需在 path 帶入 careGroupId，並在 request body 提供 title；content、logType、recordDate 可依需求填寫。")]
        public async Task<IActionResult> CreateLog(Guid careGroupId, [FromBody] CreateCareLogRequest request)
        {
            // 步驟 1：解析請求上下文 (例如：取得目前登入使用者 ID)
            var currentUserId = _currentUserService.UserId;
            
            // 步驟 2：將資料完整交接至 Service 層 (大腦) 進行邏輯運算與執行
            var result = await _careLogService.CreateLogAsync(currentUserId, careGroupId, request);
            
            // 步驟 3：整合回傳結果，定義統一格式 (ApiResponse) 與對應的 HTTP 狀態碼
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
        [EndpointDescription("更新指定的照護日誌。前端需在 path 帶入 careGroupId 與 logId，並在 request body 提供要更新的欄位與 updatedAt；updatedAt 應來自先前查詢單筆或列表 API 回傳的資料，用於樂觀鎖檢查。")]
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
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        [EndpointSummary("刪除照護日誌")]
        [EndpointDescription("刪除指定的照護日誌。前端需在 path 帶入 careGroupId 與 logId；這兩個值通常來自照護日誌列表或單筆查詢結果。此操作為 soft delete，不會物理刪除資料。")]
        public async Task<IActionResult> DeleteLog(Guid careGroupId, Guid logId)
        {
            var currentUserId = _currentUserService.UserId;
            await _careLogService.DeleteLogAsync(currentUserId, careGroupId, logId);
            var response = new ApiResponse<object>(null, "刪除照護日誌成功", HttpContext.TraceIdentifier);
            return Ok(response);
        }
    }
}
