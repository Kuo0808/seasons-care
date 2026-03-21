using System;
using System.Linq;
using System.Threading.Tasks;
using SeasonsCare.Api.DTOs.Common;
using SeasonsCare.Api.DTOs.Expenses;
using SeasonsCare.Api.Exceptions;
using SeasonsCare.Api.Models.Entities;
using SeasonsCare.Api.Repositories;

namespace SeasonsCare.Api.Services
{
    public class ExpenseService : IExpenseService
    {
        private readonly IExpenseRepository _expenseRepository;
        private readonly ICareGroupRepository _careGroupRepository;

        public ExpenseService(IExpenseRepository expenseRepository, ICareGroupRepository careGroupRepository)
        {
            _expenseRepository = expenseRepository;
            _careGroupRepository = careGroupRepository;
        }

        private async Task CheckMembershipAsync(Guid careGroupId, Guid userId)
        {
            var isMember = await _careGroupRepository.IsMemberAsync(careGroupId, userId);
            if (!isMember)
            {
                throw new DomainException("無權存取此 Care Group 的資料", "FORBIDDEN", 403);
            }
        }

        public async Task<PagedResponse<ExpenseResponse>> GetExpensesAsync(Guid currentUserId, Guid careGroupId, PaginationRequest pagination)
        {
            await CheckMembershipAsync(careGroupId, currentUserId);

            var (data, totalCount) = await _expenseRepository.GetPagedByCareGroupIdAsync(
                careGroupId, 
                pagination.Page, 
                pagination.PageSize, 
                pagination.Sort);

            var items = data.Select(MapToResponse).ToList();

            return new PagedResponse<ExpenseResponse>(items, totalCount, pagination.Page, pagination.PageSize);
        }

        public async Task<ExpenseResponse> GetExpenseByIdAsync(Guid currentUserId, Guid careGroupId, Guid expenseId)
        {
            await CheckMembershipAsync(careGroupId, currentUserId);

            var expense = await _expenseRepository.GetByIdAsync(expenseId);
            if (expense == null || expense.CareGroupId != careGroupId)
            {
                throw new DomainException("找不到此支出紀錄", "NOT_FOUND", 404);
            }

            return MapToResponse(expense);
        }

        public async Task<ExpenseResponse> CreateExpenseAsync(Guid currentUserId, Guid careGroupId, CreateExpenseRequest request)
        {
            await CheckMembershipAsync(careGroupId, currentUserId);

            var now = GetUtcNowRoundedToMilliseconds();

            var expense = new ExpenseRecord
            {
                Title = request.Title,
                Amount = request.Amount,
                Category = request.Category,
                Notes = request.Notes,
                ExpenseDate = request.ExpenseDate ?? now,
                CareGroupId = careGroupId,
                CreatedAt = now,
                UpdatedAt = now,
                CreatedBy = currentUserId.ToString()
            };

            await _expenseRepository.AddAsync(expense);
            await _expenseRepository.SaveChangesAsync();

            return MapToResponse(expense);
        }

        public async Task<ExpenseResponse> UpdateExpenseAsync(Guid currentUserId, Guid careGroupId, Guid expenseId, UpdateExpenseRequest request)
        {
            await CheckMembershipAsync(careGroupId, currentUserId);

            var expense = await _expenseRepository.GetByIdAsync(expenseId);
            if (expense == null || expense.CareGroupId != careGroupId)
            {
                throw new DomainException("找不到此支出紀錄", "NOT_FOUND", 404);
            }

            if (!request.UpdatedAt.HasValue || !expense.UpdatedAt.HasValue)
            {
                throw new DomainException("缺少併發控制資訊，請重新整理後再試", "CONCURRENCY_CONFLICT", 409);
            }

            if (NormalizeTimestamp(request.UpdatedAt.Value) != NormalizeTimestamp(expense.UpdatedAt.Value))
            {
                throw new DomainException("資料已被修改，請重新整理後再試", "CONCURRENCY_CONFLICT", 409);
            }

            expense.Title = request.Title;
            expense.Amount = request.Amount;
            expense.Category = request.Category;
            expense.Notes = request.Notes;
            if (request.ExpenseDate.HasValue)
            {
                expense.ExpenseDate = request.ExpenseDate.Value;
            }
            
            expense.UpdatedAt = GetUtcNowRoundedToMilliseconds();

            await _expenseRepository.UpdateAsync(expense);
            await _expenseRepository.SaveChangesAsync();

            return MapToResponse(expense);
        }

        public async Task DeleteExpenseAsync(Guid currentUserId, Guid careGroupId, Guid expenseId)
        {
            await CheckMembershipAsync(careGroupId, currentUserId);

            var expense = await _expenseRepository.GetByIdAsync(expenseId);
            if (expense == null || expense.CareGroupId != careGroupId)
            {
                throw new DomainException("找不到此支出紀錄", "NOT_FOUND", 404);
            }

            var now = GetUtcNowRoundedToMilliseconds();
            expense.DeletedAt = now;
            expense.UpdatedAt = now;

            await _expenseRepository.UpdateAsync(expense);
            await _expenseRepository.SaveChangesAsync();
        }

        private static ExpenseResponse MapToResponse(ExpenseRecord expense)
        {
            return new ExpenseResponse
            {
                Id = expense.Id,
                Title = expense.Title,
                Amount = expense.Amount,
                Category = expense.Category,
                Notes = expense.Notes,
                ExpenseDate = expense.ExpenseDate,
                CareGroupId = expense.CareGroupId,
                CreatedAt = expense.CreatedAt,
                UpdatedAt = expense.UpdatedAt,
                CreatedBy = expense.CreatedBy
            };
        }

        private static DateTime GetUtcNowRoundedToMilliseconds()
        {
            return NormalizeTimestamp(DateTime.UtcNow);
        }

        private static DateTime NormalizeTimestamp(DateTime value)
        {
            var utcValue = value.Kind == DateTimeKind.Utc ? value : value.ToUniversalTime();
            return new DateTime(utcValue.Ticks - (utcValue.Ticks % TimeSpan.TicksPerMillisecond), DateTimeKind.Utc);
        }
    }
}
