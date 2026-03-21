using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
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
        public async Task<IActionResult> GetExpenseById(Guid careGroupId, Guid expenseId)
        {
            var currentUserId = _currentUserService.UserId;
            var result = await _expenseService.GetExpenseByIdAsync(currentUserId, careGroupId, expenseId);
            var response = new ApiResponse<ExpenseResponse>(result, "取得支出紀錄成功", HttpContext.TraceIdentifier);
            return Ok(response);
        }

        [HttpPost]
        public async Task<IActionResult> CreateExpense(Guid careGroupId, [FromBody] CreateExpenseRequest request)
        {
            var currentUserId = _currentUserService.UserId;
            var result = await _expenseService.CreateExpenseAsync(currentUserId, careGroupId, request);
            var response = new ApiResponse<ExpenseResponse>(result, "建立支出紀錄成功", HttpContext.TraceIdentifier);
            return StatusCode(201, response);
        }

        [HttpPut("{expenseId}")]
        public async Task<IActionResult> UpdateExpense(Guid careGroupId, Guid expenseId, [FromBody] UpdateExpenseRequest request)
        {
            var currentUserId = _currentUserService.UserId;
            var result = await _expenseService.UpdateExpenseAsync(currentUserId, careGroupId, expenseId, request);
            var response = new ApiResponse<ExpenseResponse>(result, "更新支出紀錄成功", HttpContext.TraceIdentifier);
            return Ok(response);
        }

        [HttpDelete("{expenseId}")]
        public async Task<IActionResult> DeleteExpense(Guid careGroupId, Guid expenseId)
        {
            var currentUserId = _currentUserService.UserId;
            await _expenseService.DeleteExpenseAsync(currentUserId, careGroupId, expenseId);
            var response = new ApiResponse<object>(null, "刪除支出紀錄成功", HttpContext.TraceIdentifier);
            return Ok(response);
        }
    }
}
