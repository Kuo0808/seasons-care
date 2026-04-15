using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using SeasonsCare.Api.DTOs.Common;
using SeasonsCare.Api.DTOs.Expenses;
using SeasonsCare.Api.Services;

namespace SeasonsCare.Api.Controllers
{
    // [架構導覽] 展示層 (Presentation Layer) - Controller
    // 職責：負責路由定義 (Routing)、接收 HTTP 請求、驗證輸入資料 (透過 Filter) 並回傳統一格式結果。本身不處理實質的商業規則。
    /// <summary>
    /// 帳目費用 API。
    /// 管理各照護群組下的帳目與支出紀錄。
    /// </summary>
    [Authorize]
    [ApiController]
    [Route("api/care-groups/{careGroupId}/expenses")]
    public class ExpensesController : ControllerBase
    {
        private readonly IExpenseService _expenseService;
        private readonly ICurrentUserService _currentUserService;

        public ExpensesController(IExpenseService expenseService, ICurrentUserService currentUserService)
        {
            _expenseService = expenseService;
            _currentUserService = currentUserService;
        }

        [HttpGet]
        [ProducesResponseType(typeof(ApiResponse<IEnumerable<ExpenseResponse>>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [EndpointSummary("取得支出紀錄列表")]
        [EndpointDescription("取得指定照護群組底下的支出紀錄列表。前端需在 path 帶入 careGroupId，通常來自照護群組列表或目前選取中的群組；可另外用 query string 傳入 page、pageSize、sort。")]
        public async Task<IActionResult> GetExpenses(Guid careGroupId, [FromQuery] DateRangePaginationRequest paginationRequest)
        {
            var currentUserId = _currentUserService.UserId;
            var pagedResult = await _expenseService.GetExpensesAsync(currentUserId, careGroupId, paginationRequest);
            var response = new ApiResponse<IEnumerable<ExpenseResponse>>(
                pagedResult.Items,
                "取得支出紀錄列表成功",
                HttpContext.TraceIdentifier,
                pagedResult.Pagination);
            return Ok(response);
        }

        [HttpGet("{expenseId}")]
        [ProducesResponseType(typeof(ApiResponse<ExpenseResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [EndpointSummary("依照 expenseId 取得單筆支出紀錄")]
        [EndpointDescription("依照 path 參數 careGroupId 與 expenseId 取得單筆支出紀錄。前端通常會先呼叫支出列表 API，再把回傳資料中的 id 當作 expenseId 帶入；若該資料不屬於目前 careGroupId，會回傳 404。")]
        public async Task<IActionResult> GetExpenseById(Guid careGroupId, Guid expenseId)
        {
            var currentUserId = _currentUserService.UserId;
            var result = await _expenseService.GetExpenseByIdAsync(currentUserId, careGroupId, expenseId);
            var response = new ApiResponse<ExpenseResponse>(result, "取得支出紀錄成功", HttpContext.TraceIdentifier);
            return Ok(response);
        }

        [HttpPost]
        [ProducesResponseType(typeof(ApiResponse<ExpenseResponse>), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [EndpointSummary("建立支出紀錄")]
        [EndpointDescription("在指定照護群組底下建立新的支出紀錄。前端需在 path 帶入 careGroupId，並在 request body 提供 title、amount、category、expenseDate；notes 與 splitStatus 為選填。category 支援 medical、food、traffic、other；splitStatus 若省略，後端會預設為 none，且僅接受 pending、settled、none 這三種字串值。")]
        public async Task<IActionResult> CreateExpense(Guid careGroupId, [FromBody] CreateExpenseRequest request)
        {
            // 步驟 1：解析請求上下文 (取得目前登入使用者 ID)
            var currentUserId = _currentUserService.UserId;
            // 步驟 2：將請求參數轉交給 Service 層 (大腦) 處理實質的商業邏輯
            var result = await _expenseService.CreateExpenseAsync(currentUserId, careGroupId, request);
            // 步驟 3：包裝為專案統一標準的 ApiResponse 並回傳對應的 HTTP 狀態碼
            var response = new ApiResponse<ExpenseResponse>(result, "建立支出紀錄成功", HttpContext.TraceIdentifier);
            return StatusCode(201, response);
        }

        [HttpPut("{expenseId}")]
        [ProducesResponseType(typeof(ApiResponse<ExpenseResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        [EndpointSummary("更新支出紀錄")]
        [EndpointDescription("更新指定的支出紀錄。前端需在 path 帶入 careGroupId 與 expenseId，並在 request body 提供 title、amount、category、expenseDate、updatedAt；notes 與 splitStatus 為選填。updatedAt 應來自先前查詢單筆或列表 API 回傳的資料，用於樂觀鎖檢查。category 支援 medical、food、traffic、other；splitStatus 若省略，後端會預設為 none，且僅接受 pending、settled、none 這三種字串值。")]
        public async Task<IActionResult> UpdateExpense(Guid careGroupId, Guid expenseId, [FromBody] UpdateExpenseRequest request)
        {
            var currentUserId = _currentUserService.UserId;
            var result = await _expenseService.UpdateExpenseAsync(currentUserId, careGroupId, expenseId, request);
            var response = new ApiResponse<ExpenseResponse>(result, "更新支出紀錄成功", HttpContext.TraceIdentifier);
            return Ok(response);
        }

        [HttpDelete("{expenseId}")]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        [EndpointSummary("刪除支出紀錄")]
        [EndpointDescription("刪除指定的支出紀錄。前端需在 path 帶入 careGroupId 與 expenseId；這兩個值通常來自支出列表或單筆查詢結果。此操作為 soft delete，不會物理刪除資料。")]
        public async Task<IActionResult> DeleteExpense(Guid careGroupId, Guid expenseId)
        {
            var currentUserId = _currentUserService.UserId;
            await _expenseService.DeleteExpenseAsync(currentUserId, careGroupId, expenseId);
            var response = new ApiResponse<object>(null, "刪除支出紀錄成功", HttpContext.TraceIdentifier);
            return Ok(response);
        }

        [HttpPost("split-preview")]
        [ProducesResponseType(typeof(ApiResponse<ExpenseSplitPreviewResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [EndpointSummary("預覽一鍵分帳結果")]
        [EndpointDescription("前端傳入想要分帳的項目 (ExpenseIds) 以及參與分攤的成員 (TargetUserIds)，後端即時計算各成員的應收與應付金額並回傳，不會更動資料庫狀態。")]
        public async Task<IActionResult> PreviewSplit(Guid careGroupId, [FromBody] SplitPreviewRequest request)
        {
            var currentUserId = _currentUserService.UserId;
            var result = await _expenseService.PreviewSplitAsync(currentUserId, careGroupId, request);
            var response = new ApiResponse<ExpenseSplitPreviewResponse>(result, "取得分帳預覽成功", HttpContext.TraceIdentifier);
            return Ok(response);
        }

        [HttpPost("split")]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [EndpointSummary("確認一鍵分帳")]
        [EndpointDescription("將傳入的特定支出項目 (ExpenseIds) 狀態變更為 Settled (已結清)。請在呼叫此 API 前，藉由預覽 API 確認分帳結果。")]
        public async Task<IActionResult> ConfirmSplit(Guid careGroupId, [FromBody] SplitConfirmRequest request)
        {
            var currentUserId = _currentUserService.UserId;
            await _expenseService.ConfirmSplitAsync(currentUserId, careGroupId, request);
            var response = new ApiResponse<object>(null, "確認分帳成功", HttpContext.TraceIdentifier);
            return Ok(response);
        }
    }
}
