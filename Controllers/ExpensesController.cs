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
        [EndpointSummary("取得支出列表")]
        [EndpointDescription("取得指定照護群組的支出列表。")]
        public async Task<IActionResult> GetExpenses(Guid careGroupId, [FromQuery] DateRangePaginationRequest paginationRequest)
        {
            var currentUserId = _currentUserService.UserId;
            var pagedResult = await _expenseService.GetExpensesAsync(currentUserId, careGroupId, paginationRequest);
            var response = new ApiResponse<IEnumerable<ExpenseResponse>>(
                pagedResult.Items,
                "取得支出列表成功",
                HttpContext.TraceIdentifier,
                pagedResult.Pagination);
            return Ok(response);
        }

        [HttpGet("{expenseId}")]
        [ProducesResponseType(typeof(ApiResponse<ExpenseResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [EndpointSummary("取得單筆支出")]
        [EndpointDescription("依照 expenseId 取得單筆支出紀錄。")]
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
        [EndpointSummary("建立支出")]
        [EndpointDescription("在指定照護群組底下建立新的支出紀錄。")]
        public async Task<IActionResult> CreateExpense(Guid careGroupId, [FromBody] CreateExpenseRequest request)
        {
            var currentUserId = _currentUserService.UserId;
            var result = await _expenseService.CreateExpenseAsync(currentUserId, careGroupId, request);
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
        [EndpointSummary("更新支出")]
        [EndpointDescription("更新指定的支出紀錄。")]
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
        [EndpointSummary("刪除支出")]
        [EndpointDescription("刪除指定的支出紀錄。")]
        public async Task<IActionResult> DeleteExpense(Guid careGroupId, Guid expenseId)
        {
            var currentUserId = _currentUserService.UserId;
            await _expenseService.DeleteExpenseAsync(currentUserId, careGroupId, expenseId);
            var response = new ApiResponse<object>(null, "刪除支出紀錄成功", HttpContext.TraceIdentifier);
            return Ok(response);
        }

        [HttpGet("member-totals")]
        [ProducesResponseType(typeof(ApiResponse<MemberExpenseTotalsResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [EndpointSummary("取得成員累積花費")]
        [EndpointDescription("回傳指定照護群組內每位成員的累積金額。")]
        public async Task<IActionResult> GetMemberTotals(Guid careGroupId, [FromQuery] MemberExpenseTotalsRequest request)
        {
            var currentUserId = _currentUserService.UserId;
            var result = await _expenseService.GetMemberExpenseTotalsAsync(currentUserId, careGroupId, request);
            var response = new ApiResponse<MemberExpenseTotalsResponse>(result, "取得各成員累積花費成功", HttpContext.TraceIdentifier);
            return Ok(response);
        }

        [HttpGet("split-preview")]
        [ProducesResponseType(typeof(ApiResponse<ExpenseSplitPreviewResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [EndpointSummary("取得分帳預覽畫面資料")]
        [EndpointDescription("提供 GET 版分帳預覽資料，回傳畫面需要的帳目明細、總額、筆數與每位成員的預覽分帳結果。會自動以照護群組目前有效成員作為分帳對象。支援 daily、monthly、custom 三種 splitMode；daily / monthly 可搭配 targetDate，custom 則以 query string 中的 expenseIds 指定帳目。")]
        public async Task<IActionResult> GetSplitPreview(Guid careGroupId, [FromQuery] SplitPreviewQueryRequest request)
        {
            var currentUserId = _currentUserService.UserId;
            var result = await _expenseService.GetSplitPreviewAsync(currentUserId, careGroupId, request);
            var response = new ApiResponse<ExpenseSplitPreviewResponse>(result, "取得分帳預覽資料成功", HttpContext.TraceIdentifier);
            return Ok(response);
        }

        [HttpPost("split-preview")]
        [ProducesResponseType(typeof(ApiResponse<ExpenseSplitPreviewResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [EndpointSummary("預覽一鍵分帳結果")]
        [EndpointDescription("支援 daily、monthly、custom 三種分帳模式，回傳即時計算後的分帳預覽，不會更動資料庫狀態。")]
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
        [EndpointDescription("確認分帳後會將待分帳支出結算並寫入分帳明細。")]
        public async Task<IActionResult> ConfirmSplit(Guid careGroupId, [FromBody] SplitConfirmRequest request)
        {
            var currentUserId = _currentUserService.UserId;
            await _expenseService.ConfirmSplitAsync(currentUserId, careGroupId, request);
            var response = new ApiResponse<object>(null, "確認分帳成功", HttpContext.TraceIdentifier);
            return Ok(response);
        }
    }
}
