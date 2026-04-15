using System;
using System.Linq;
using System.Threading.Tasks;
using SeasonsCare.Api.Config;
using SeasonsCare.Api.DTOs.Common;
using SeasonsCare.Api.DTOs.Expenses;
using SeasonsCare.Api.Exceptions;
using SeasonsCare.Api.Models.Entities;
using SeasonsCare.Api.Models.Enums;
using SeasonsCare.Api.Repositories;

namespace SeasonsCare.Api.Services
{
    // [架構導覽] 商業邏輯層 (Business Logic Layer) - Service
    // 職責：系統的核心大腦。負責執行特定領域規則 (Domain Rules)、查核權限、進行資料映射與轉換。完成驗證後方能呼叫 Repository。
    public class ExpenseService : IExpenseService
    {
        private readonly IExpenseRepository _expenseRepository;
        private readonly ICareGroupRepository _careGroupRepository;
        private readonly IUserRepository _userRepository;

        public ExpenseService(IExpenseRepository expenseRepository, ICareGroupRepository careGroupRepository, IUserRepository userRepository)
        {
            _expenseRepository = expenseRepository;
            _careGroupRepository = careGroupRepository;
            _userRepository = userRepository;
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

            // 列表查詢改以日期區間為主，避免載入整包歷史分帳資料。
            var request = pagination.ToDateRangeRequest();
            var (data, totalCount) = await _expenseRepository.GetPagedByCareGroupIdAsync(careGroupId, request);

            var items = data.Select(MapToResponse).ToList();

            return new PagedResponse<ExpenseResponse>(items, totalCount, request.Page, request.PageSize);
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
                ExpenseDate = NormalizeTimestamp(request.ExpenseDate),
                SplitStatus = request.SplitStatus,
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
            expense.ExpenseDate = NormalizeTimestamp(request.ExpenseDate);
            expense.SplitStatus = request.SplitStatus;
            
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

        public async Task<ExpenseSplitPreviewResponse> PreviewSplitAsync(Guid currentUserId, Guid careGroupId, SplitPreviewRequest request)
        {
            await CheckMembershipAsync(careGroupId, currentUserId);

            var expenses = await _expenseRepository.GetListByIdsAsync(careGroupId, request.ExpenseIds);

            // 確保有找到任何未分帳的項目
            var validExpenses = expenses.Where(e => e.SplitStatus != ExpenseSplitStatus.Settled).ToList();
            if (!validExpenses.Any())
            {
                throw new DomainException("沒有找到可分帳的有效支出紀錄", "BAD_REQUEST", 400);
            }

            var totalAmount = validExpenses.Sum(e => e.Amount);
            var usersCount = request.TargetUserIds.Count;
            if (usersCount == 0)
            {
                throw new DomainException("請至少選擇一位參與分攤的使用者", "BAD_REQUEST", 400);
            }

            // 平均分攤金額 (無條件捨去至小數第二位等計算依需求，此處使用精確除法)
            var sharePerPerson = Math.Round(totalAmount / usersCount, 2);

            // 取得分攤者的 User 資料
            var loadedUsers = await _userRepository.GetListByIdsAsync(request.TargetUserIds);

            // 統計每個人已付了多少
            var paidAmounts = new Dictionary<Guid, decimal>();
            foreach (var userId in request.TargetUserIds)
            {
                paidAmounts[userId] = 0;
            }

            foreach (var exp in validExpenses)
            {
                if (Guid.TryParse(exp.CreatedBy, out var payerId) && paidAmounts.ContainsKey(payerId))
                {
                    paidAmounts[payerId] += exp.Amount;
                }
            }

            var splitDetails = new List<SplitUserDetail>();

            foreach (var user in loadedUsers)
            {
                var paidByThisUser = paidAmounts.GetValueOrDefault(user.Id, 0m);
                var balance = paidByThisUser - sharePerPerson; // 正數代表他付的多，應該回收；負數代表他付的少，應該補錢

                splitDetails.Add(new SplitUserDetail
                {
                    UserId = user.Id,
                    Name = user.Username, // 依據您的系統，有可能叫 Username 或 DisplayName
                    AvatarUrl = user.AvatarKey, // 您可能需要轉換 Key 為 URL，先以設計稿 MVP 回傳此原始值
                    IsPayer = paidByThisUser > 0,
                    ReceivableAmount = balance > 0 ? balance : 0,
                    PayableAmount = balance < 0 ? Math.Abs(balance) : 0
                });
            }

            var currentTargetUserIds = loadedUsers.Select(u => u.Id).ToList();
            // 對於沒被拉進分帳對象但卻付了錢的例外情況 (這裡簡單處理：依設計稿前端會選好對象)

            return new ExpenseSplitPreviewResponse
            {
                TotalAmount = totalAmount,
                SelectedExpenses = validExpenses.Select(e => new ExpenseItemSummary
                {
                    Id = e.Id,
                    Title = e.Title,
                    Amount = e.Amount
                }).ToList(),
                SplitDetails = splitDetails
            };
        }

        public async Task ConfirmSplitAsync(Guid currentUserId, Guid careGroupId, SplitConfirmRequest request)
        {
            await CheckMembershipAsync(careGroupId, currentUserId);

            var expenses = await _expenseRepository.GetListByIdsAsync(careGroupId, request.ExpenseIds);

            var validExpenses = expenses.Where(e => e.SplitStatus != ExpenseSplitStatus.Settled).ToList();
            if (!validExpenses.Any())
            {
                throw new DomainException("沒有找到需結算的分帳項目", "BAD_REQUEST", 400);
            }

            var now = GetUtcNowRoundedToMilliseconds();

            foreach (var exp in validExpenses)
            {
                exp.SplitStatus = ExpenseSplitStatus.Settled;
                exp.UpdatedAt = now;
                await _expenseRepository.UpdateAsync(exp);
            }

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
                Notes = expense.Notes ?? string.Empty,
                ExpenseDate = TimeHelper.ToTaiwanOffset(expense.ExpenseDate),
                SplitStatus = expense.SplitStatus,
                CareGroupId = expense.CareGroupId,
                CreatedAt = TimeHelper.ToTaiwanOffset(expense.CreatedAt),
                UpdatedAt = TimeHelper.ToTaiwanOffset(expense.UpdatedAt ?? expense.CreatedAt),
                CreatedBy = expense.CreatedBy
            };
        }

        private static DateTime GetUtcNowRoundedToMilliseconds()
        {
            return NormalizeTimestamp(TimeHelper.UtcNow);
        }

        private static DateTime NormalizeTimestamp(DateTime value)
        {
            var utcValue = value.Kind == DateTimeKind.Utc ? value : value.ToUniversalTime();
            return new DateTime(utcValue.Ticks - (utcValue.Ticks % TimeSpan.TicksPerMillisecond), DateTimeKind.Utc);
        }
    }
}
