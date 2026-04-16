using System;
using System.Collections.Generic;
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
        private readonly IExpenseSplitRepository _expenseSplitRepository;

        public ExpenseService(IExpenseRepository expenseRepository, ICareGroupRepository careGroupRepository, IUserRepository userRepository, IExpenseSplitRepository expenseSplitRepository)
        {
            _expenseRepository = expenseRepository;
            _careGroupRepository = careGroupRepository;
            _userRepository = userRepository;
            _expenseSplitRepository = expenseSplitRepository;
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

            var validExpenses = await ResolveExpensesAsync(careGroupId, request.SplitMode, request.ExpenseIds, request.TargetDate);
            if (validExpenses.Count == 0)
            {
                throw new DomainException("沒有找到可分帳的有效支出紀錄", "BAD_REQUEST", 400);
            }

            var usersCount = request.TargetUserIds.Count;
            if (usersCount == 0)
            {
                throw new DomainException("請至少選擇一位參與分攤的使用者", "BAD_REQUEST", 400);
            }

            var totalAmount = validExpenses.Sum(e => e.Amount);
            var sharePerPerson = Math.Round(totalAmount / usersCount, 2);

            var loadedUsers = await _userRepository.GetListByIdsAsync(request.TargetUserIds);

            // 統計每個人已付了多少
            var paidAmounts = request.TargetUserIds.ToDictionary(id => id, _ => 0m);
            foreach (var exp in validExpenses)
            {
                if (Guid.TryParse(exp.CreatedBy, out var payerId) && paidAmounts.ContainsKey(payerId))
                {
                    paidAmounts[payerId] += exp.Amount;
                }
            }

            var splitDetails = loadedUsers.Select(user =>
            {
                var paidByThisUser = paidAmounts.GetValueOrDefault(user.Id, 0m);
                var balance = paidByThisUser - sharePerPerson;
                return new SplitUserDetail
                {
                    UserId = user.Id,
                    Name = user.Username,
                    AvatarUrl = user.AvatarKey,
                    IsPayer = paidByThisUser > 0,
                    ReceivableAmount = balance > 0 ? balance : 0,
                    PayableAmount = balance < 0 ? Math.Abs(balance) : 0
                };
            }).ToList();

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

            var validExpenses = await ResolveExpensesAsync(careGroupId, request.SplitMode, request.ExpenseIds, request.TargetDate);
            if (validExpenses.Count == 0)
            {
                throw new DomainException("沒有找到需結算的分帳項目", "BAD_REQUEST", 400);
            }

            var targetUserIds = (request.TargetUserIds ?? new List<Guid>()).Distinct().ToList();
            if (targetUserIds.Count == 0)
            {
                throw new DomainException("請至少選擇一位參與分攤的使用者", "BAD_REQUEST", 400);
            }

            var now = GetUtcNowRoundedToMilliseconds();
            var createdBy = currentUserId.ToString();

            // 1. 把每筆 expense 標為 Settled，並為每位 targetUser 寫入分攤明細。
            // 2. 平均分攤：share = amount / count（四捨五入到分），最後一位吸收尾差，避免 N 列加總後因為 round 偏差。
            foreach (var exp in validExpenses)
            {
                exp.SplitStatus = ExpenseSplitStatus.Settled;
                exp.UpdatedAt = now;
                await _expenseRepository.UpdateAsync(exp);

                Guid? payerId = Guid.TryParse(exp.CreatedBy, out var parsedPayer) ? parsedPayer : null;
                var splits = BuildSplitsForExpense(exp, targetUserIds, payerId, createdBy, now);
                await _expenseSplitRepository.AddRangeAsync(splits);
            }

            await _expenseRepository.SaveChangesAsync();
            await _expenseSplitRepository.SaveChangesAsync();
        }

        /// <summary>
        /// 把單筆 ExpenseRecord 展開為 N 筆 ExpenseSplit。
        /// 平均分攤：先全部四捨五入到 0.01，最後一筆吸收尾差，確保 sum(splits.ShareAmount) == expense.Amount。
        /// </summary>
        private static List<ExpenseSplit> BuildSplitsForExpense(
            ExpenseRecord expense,
            List<Guid> targetUserIds,
            Guid? payerId,
            string createdBy,
            DateTime now)
        {
            var count = targetUserIds.Count;
            var baseShare = Math.Round(expense.Amount / count, 2, MidpointRounding.AwayFromZero);
            var splits = new List<ExpenseSplit>(count);

            for (int i = 0; i < count; i++)
            {
                var userId = targetUserIds[i];
                var share = (i == count - 1)
                    ? expense.Amount - baseShare * (count - 1) // 最後一位吸收 round 尾差
                    : baseShare;

                splits.Add(new ExpenseSplit
                {
                    ExpenseId = expense.Id,
                    UserId = userId,
                    ShareAmount = share,
                    IsPayer = payerId.HasValue && payerId.Value == userId,
                    CareGroupId = expense.CareGroupId,
                    CreatedAt = now,
                    UpdatedAt = now,
                    CreatedBy = createdBy
                });
            }

            return splits;
        }

        /// <summary>
        /// 依據分帳模式取得要處理的費用清單，僅回傳「待分帳（Pending）」的項目。
        /// daily：撈指定日（台灣時區，預設今天）所有 Pending 費用。
        /// monthly：撈指定月（台灣時區，預設本月）所有 Pending 費用。
        /// custom：依 ExpenseIds 撈取，僅保留 Pending。
        /// </summary>
        private async Task<List<ExpenseRecord>> ResolveExpensesAsync(Guid careGroupId, string splitMode, List<Guid>? expenseIds, DateTime? targetDate)
        {
            var mode = (splitMode ?? "custom").Trim().ToLowerInvariant();

            if (mode == "daily")
            {
                var anchorTaiwanDate = ResolveTaiwanAnchorDate(targetDate);
                var dayStartTaiwan = new DateTime(anchorTaiwanDate.Year, anchorTaiwanDate.Month, anchorTaiwanDate.Day, 0, 0, 0, DateTimeKind.Unspecified);
                var dayEndTaiwan = dayStartTaiwan.AddDays(1);
                var dayStartUtc = TimeHelper.TaiwanToUtc(dayStartTaiwan);
                var dayEndUtc = TimeHelper.TaiwanToUtc(dayEndTaiwan);
                return await _expenseRepository.GetPendingByDateRangeAsync(careGroupId, dayStartUtc, dayEndUtc);
            }

            if (mode == "monthly")
            {
                var anchorTaiwanDate = ResolveTaiwanAnchorDate(targetDate);
                var monthStartTaiwan = new DateTime(anchorTaiwanDate.Year, anchorTaiwanDate.Month, 1, 0, 0, 0, DateTimeKind.Unspecified);
                var monthEndTaiwan = monthStartTaiwan.AddMonths(1);
                var monthStartUtc = TimeHelper.TaiwanToUtc(monthStartTaiwan);
                var monthEndUtc = TimeHelper.TaiwanToUtc(monthEndTaiwan);
                return await _expenseRepository.GetPendingByDateRangeAsync(careGroupId, monthStartUtc, monthEndUtc);
            }

            // custom 模式：依 ExpenseIds 撈取，僅保留 Pending（排除 None 與 Settled）。
            if (expenseIds == null || expenseIds.Count == 0)
            {
                throw new DomainException("自選模式下請提供至少一筆支出項目", "BAD_REQUEST", 400);
            }

            var expenses = await _expenseRepository.GetListByIdsAsync(careGroupId, expenseIds);
            return expenses.Where(e => e.SplitStatus == ExpenseSplitStatus.Pending).ToList();
        }

        public async Task<MemberExpenseTotalsResponse> GetMemberExpenseTotalsAsync(Guid currentUserId, Guid careGroupId, MemberExpenseTotalsRequest request)
        {
            await CheckMembershipAsync(careGroupId, currentUserId);

            // 1. 解析統計範圍 → 取得（可能為 null 的）UTC 日期區間
            var (dateFromUtc, dateToUtc) = ResolveScopeRange(request.Scope, request.TargetDate);
            var members = await _careGroupRepository.GetActiveMembersWithUserAsync(careGroupId);

            // 2. 依 viewMode 取得每位 user 的（金額,筆數）
            var viewMode = (request.ViewMode ?? "payer").Trim().ToLowerInvariant();
            var totalsByUser = viewMode == "share"
                ? await BuildShareTotalsAsync(careGroupId, dateFromUtc, dateToUtc)
                : await BuildPayerTotalsAsync(careGroupId, dateFromUtc, dateToUtc, request.PendingOnly);

            // 3. 組裝每位成員的明細（沒對應紀錄的也要回傳，金額 0）
            var items = members
                .Where(m => m.User != null)
                .Select(m =>
                {
                    var stat = totalsByUser.TryGetValue(m.UserId, out var v) ? v : (Total: 0m, Count: 0);
                    return new MemberExpenseTotalItem
                    {
                        UserId = m.UserId,
                        Name = m.User.Username,
                        AvatarUrl = string.IsNullOrWhiteSpace(m.User.AvatarKey) ? null : m.User.AvatarKey,
                        TotalAmount = stat.Total,
                        ExpenseCount = stat.Count
                    };
                })
                .OrderByDescending(x => x.TotalAmount)
                .ThenBy(x => x.Name)
                .ToList();

            return new MemberExpenseTotalsResponse
            {
                TotalAmount = items.Sum(x => x.TotalAmount),
                MemberCount = items.Count,
                Members = items
            };
        }

        /// <summary>
        /// payer 視角：以 ExpenseRecord.CreatedBy（付款人）為 key 聚合金額。
        /// </summary>
        private async Task<Dictionary<Guid, (decimal Total, int Count)>> BuildPayerTotalsAsync(
            Guid careGroupId, DateTime? dateFromUtc, DateTime? dateToUtc, bool pendingOnly)
        {
            var statusFilter = pendingOnly ? (ExpenseSplitStatus?)ExpenseSplitStatus.Pending : null;
            var expenses = await _expenseRepository.GetByCareGroupAsync(careGroupId, dateFromUtc, dateToUtc, statusFilter);

            return expenses
                .Where(e => Guid.TryParse(e.CreatedBy, out _))
                .GroupBy(e => Guid.Parse(e.CreatedBy))
                .ToDictionary(g => g.Key, g => (Total: g.Sum(x => x.Amount), Count: g.Count()));
        }

        /// <summary>
        /// share 視角：以 ExpenseSplit.UserId（被分攤對象）為 key 聚合 ShareAmount。
        /// 分帳明細只會在「確認分帳」後產生，因此不需考慮 PendingOnly。
        /// </summary>
        private async Task<Dictionary<Guid, (decimal Total, int Count)>> BuildShareTotalsAsync(
            Guid careGroupId, DateTime? dateFromUtc, DateTime? dateToUtc)
        {
            var splits = await _expenseSplitRepository.GetByCareGroupAsync(careGroupId, dateFromUtc, dateToUtc);

            return splits
                .GroupBy(s => s.UserId)
                .ToDictionary(g => g.Key, g => (Total: g.Sum(x => x.ShareAmount), Count: g.Count()));
        }

        /// <summary>
        /// 解析統計範圍：
        /// daily / monthly：依 targetDate（台灣時區，預設今天）算出 UTC 區間。
        /// all：回傳 (null, null) 表示不限制日期。
        /// </summary>
        private static (DateTime? DateFromUtc, DateTime? DateToUtc) ResolveScopeRange(string? scope, DateTime? targetDate)
        {
            var mode = (scope ?? "monthly").Trim().ToLowerInvariant();

            if (mode == "all")
            {
                return (null, null);
            }

            var anchorTaiwanDate = ResolveTaiwanAnchorDate(targetDate);

            if (mode == "daily")
            {
                var dayStartTaiwan = new DateTime(anchorTaiwanDate.Year, anchorTaiwanDate.Month, anchorTaiwanDate.Day, 0, 0, 0, DateTimeKind.Unspecified);
                var dayEndTaiwan = dayStartTaiwan.AddDays(1);
                return (TimeHelper.TaiwanToUtc(dayStartTaiwan), TimeHelper.TaiwanToUtc(dayEndTaiwan));
            }

            // monthly（預設）
            var monthStartTaiwan = new DateTime(anchorTaiwanDate.Year, anchorTaiwanDate.Month, 1, 0, 0, 0, DateTimeKind.Unspecified);
            var monthEndTaiwan = monthStartTaiwan.AddMonths(1);
            return (TimeHelper.TaiwanToUtc(monthStartTaiwan), TimeHelper.TaiwanToUtc(monthEndTaiwan));
        }

        /// <summary>
        /// 將呼叫端傳入的目標日期轉為台灣時區的日期。未提供時預設為「今天（台灣時區）」。
        /// 接受任何 Kind 的 DateTime：Utc 會自動轉台灣；Local / Unspecified 視為台灣當地日期。
        /// </summary>
        private static DateTime ResolveTaiwanAnchorDate(DateTime? targetDate)
        {
            if (!targetDate.HasValue)
            {
                return TimeHelper.ToTaiwanTime(TimeHelper.UtcNow).Date;
            }

            var value = targetDate.Value;
            if (value.Kind == DateTimeKind.Utc)
            {
                return TimeHelper.ToTaiwanTime(value).Date;
            }

            // Unspecified / Local 一律視為使用者指定的台灣當地日期。
            return value.Date;
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
