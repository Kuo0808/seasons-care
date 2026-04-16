using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using SeasonsCare.Api.DTOs.Common;
using SeasonsCare.Api.DTOs.Expenses;

namespace SeasonsCare.Api.Services
{
    public interface IExpenseService
    {
        Task<PagedResponse<ExpenseResponse>> GetExpensesAsync(Guid currentUserId, Guid careGroupId, PaginationRequest pagination);
        Task<ExpenseResponse> GetExpenseByIdAsync(Guid currentUserId, Guid careGroupId, Guid expenseId);
        Task<ExpenseResponse> CreateExpenseAsync(Guid currentUserId, Guid careGroupId, CreateExpenseRequest request);
        Task<ExpenseResponse> UpdateExpenseAsync(Guid currentUserId, Guid careGroupId, Guid expenseId, UpdateExpenseRequest request);
        Task DeleteExpenseAsync(Guid currentUserId, Guid careGroupId, Guid expenseId);
        Task<ExpenseSplitPreviewResponse> PreviewSplitAsync(Guid currentUserId, Guid careGroupId, SplitPreviewRequest request);
        Task ConfirmSplitAsync(Guid currentUserId, Guid careGroupId, SplitConfirmRequest request);
        Task<MemberExpenseTotalsResponse> GetMemberExpenseTotalsAsync(Guid currentUserId, Guid careGroupId, MemberExpenseTotalsRequest request);
    }
}
