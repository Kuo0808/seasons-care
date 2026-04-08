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
    // [架構導覽] 商業邏輯層 (Business Logic Layer) - Service
    // 職責：系統的核心大腦。負責執行特定領域規則 (Domain Rules)、查核權限、進行資料映射與轉換。完成驗證後方能呼叫 Repository。
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
            // 步驟 1：執行前置邏輯校驗與權限審核
            await CheckMembershipAsync(careGroupId, currentUserId);

            var now = GetUtcNowRoundedToMilliseconds();

            // 步驟 2：將前端請求 DTO (Data Transfer Object) 封裝為標準資料庫實體 Entity
            var expense = new ExpenseRecord
            {
                Title = request.Title,
                Amount = request.Amount,
                Category = request.Category,
                Notes = request.Notes,
                ExpenseDate = request.ExpenseDate ?? now,
                IsSplitRequired = request.IsSplitRequired,
                CareGroupId = careGroupId,
                CreatedAt = now,
                UpdatedAt = now,
                CreatedBy = currentUserId.ToString()
            };

            // 步驟 3：透過 Repository 層將 Entity 保存入庫
            await _expenseRepository.AddAsync(expense);
            await _expenseRepository.SaveChangesAsync();

            // 步驟 4：將結果進行資料映射 (Map to Response)，不把內部 Entity 直接曝露給前端
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
            expense.IsSplitRequired = request.IsSplitRequired;
            
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
                IsSplitRequired = expense.IsSplitRequired,
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
