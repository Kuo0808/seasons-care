using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SeasonsCare.Api.DTOs.Common;
using SeasonsCare.Api.DTOs.Expenses;
using Microsoft.AspNetCore.Http;
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
        [EndpointSummary("取得支出紀錄列表")]
        [EndpointDescription("取得指定照護群組下的支出紀錄列表，支援分頁參數。")]
        public async Task<IActionResult> GetExpenses(Guid careGroupId, [FromQuery] PaginationRequest paginationRequest)
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
        [EndpointSummary("取得單筆支出紀錄")]
        [EndpointDescription("依照支出 ID 取得單筆花費紀錄。")]
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
        [EndpointDescription("在指定的照護群組內建立一筆新的支出花費紀錄。")]
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
        [EndpointSummary("更新支出紀錄")]
        [EndpointDescription("更新既有的支出紀錄內容。前端需帶入 updatedAt 作為樂觀鎖判定。")]
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
        [EndpointSummary("刪除支出紀錄")]
        [EndpointDescription("刪除指定的支出紀錄（Soft Delete）。")]
        public async Task<IActionResult> DeleteExpense(Guid careGroupId, Guid expenseId)
        {
            var currentUserId = _currentUserService.UserId;
            await _expenseService.DeleteExpenseAsync(currentUserId, careGroupId, expenseId);
            var response = new ApiResponse<object>(null, "刪除支出紀錄成功", HttpContext.TraceIdentifier);
            return Ok(response);
        }
    }
}
