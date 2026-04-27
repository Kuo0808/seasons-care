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
    public class ExpenseService : IExpenseService
    {
        private readonly IExpenseRepository _expenseRepository;
        private readonly ICareGroupRepository _careGroupRepository;
        private readonly IUserRepository _userRepository;
        private readonly IExpenseSplitRepository _expenseSplitRepository;

        public ExpenseService(
            IExpenseRepository expenseRepository,
            ICareGroupRepository careGroupRepository,
            IUserRepository userRepository,
            IExpenseSplitRepository expenseSplitRepository)
        {
            _expenseRepository = expenseRepository;
            _careGroupRepository = careGroupRepository;
            _userRepository = userRepository;
            _expenseSplitRepository = expenseSplitRepository;
        }

        public async Task<PagedResponse<ExpenseResponse>> GetExpensesAsync(Guid currentUserId, Guid careGroupId, PaginationRequest pagination)
        {
            await CheckMembershipAsync(careGroupId, currentUserId);

            var request = pagination.ToDateRangeRequest();
            var (data, totalCount) = await _expenseRepository.GetPagedByCareGroupIdAsync(careGroupId, request);

            var settledIds = data
                .Where(expense => expense.SplitStatus == ExpenseSplitStatus.Settled)
                .Select(expense => expense.Id)
                .ToList();
            var batchIdMap = settledIds.Count == 0
                ? new Dictionary<Guid, Guid>()
                : await _expenseSplitRepository.GetBatchIdsByExpenseIdsAsync(careGroupId, settledIds);

            var items = data
                .Select(expense => MapToResponse(
                    expense,
                    batchIdMap.TryGetValue(expense.Id, out var batchId) ? batchId : (Guid?)null))
                .ToList();

            return new PagedResponse<ExpenseResponse>(items, totalCount, request.Page, request.PageSize);
        }

        public async Task<ExpenseResponse> GetExpenseByIdAsync(Guid currentUserId, Guid careGroupId, Guid expenseId)
        {
            await CheckMembershipAsync(careGroupId, currentUserId);

            var expense = await _expenseRepository.GetByIdAsync(expenseId);
            if (expense == null || expense.CareGroupId != careGroupId)
            {
                throw new DomainException("找不到支出紀錄", "NOT_FOUND", 404);
            }

            Guid? splitBatchId = null;
            if (expense.SplitStatus == ExpenseSplitStatus.Settled)
            {
                var batchIdMap = await _expenseSplitRepository.GetBatchIdsByExpenseIdsAsync(careGroupId, new[] { expense.Id });
                if (batchIdMap.TryGetValue(expense.Id, out var batchId))
                {
                    splitBatchId = batchId;
                }
            }

            return MapToResponse(expense, splitBatchId);
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
                ExpenseDate = NormalizeTimestamp(request.ExpenseDate),
                SplitStatus = request.SplitStatus,
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
                throw new DomainException("找不到支出紀錄", "NOT_FOUND", 404);
            }

            if (!request.UpdatedAt.HasValue || !expense.UpdatedAt.HasValue)
            {
                throw new DomainException("缺少更新比對時間，請重新整理後再試", "CONCURRENCY_CONFLICT", 409);
            }

            if (NormalizeTimestamp(request.UpdatedAt.Value) != NormalizeTimestamp(expense.UpdatedAt.Value))
            {
                throw new DomainException("資料已被更新，請重新整理後再試", "CONCURRENCY_CONFLICT", 409);
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
                throw new DomainException("找不到支出紀錄", "NOT_FOUND", 404);
            }

            var now = GetUtcNowRoundedToMilliseconds();
            expense.DeletedAt = now;
            expense.UpdatedAt = now;

            await _expenseRepository.UpdateAsync(expense);
            await _expenseRepository.SaveChangesAsync();
        }

        public async Task<ExpenseSplitPreviewResponse> GetSplitPreviewAsync(Guid currentUserId, Guid careGroupId, SplitPreviewQueryRequest request)
        {
            await CheckMembershipAsync(careGroupId, currentUserId);

            if (request.SplitBatchId.HasValue)
            {
                return await BuildSettledSplitResultAsync(careGroupId, request.SplitBatchId.Value);
            }

            var participants = (await _careGroupRepository.GetActiveMembersWithUserAsync(careGroupId))
                .Where(member => member.User != null)
                .GroupBy(member => member.UserId)
                .Select(group => group.First().User!)
                .ToList();

            if (participants.Count == 0)
            {
                throw new DomainException("查無可用的分帳成員", "BAD_REQUEST", 400);
            }

            return await BuildSplitPreviewAsync(careGroupId, request.SplitMode, request.ExpenseIds, request.TargetDate, participants);
        }

        public async Task<ExpenseSplitPreviewResponse> PreviewSplitAsync(Guid currentUserId, Guid careGroupId, SplitPreviewRequest request)
        {
            await CheckMembershipAsync(careGroupId, currentUserId);

            if (request.TargetUserIds.Count == 0)
            {
                throw new DomainException("請至少提供一位分帳成員", "BAD_REQUEST", 400);
            }

            var participants = await _userRepository.GetListByIdsAsync(request.TargetUserIds);
            return await BuildSplitPreviewAsync(careGroupId, request.SplitMode, request.ExpenseIds, request.TargetDate, participants);
        }

        public async Task<SplitConfirmResponse> ConfirmSplitAsync(Guid currentUserId, Guid careGroupId, SplitConfirmRequest request)
        {
            await CheckMembershipAsync(careGroupId, currentUserId);

            var validExpenses = await ResolveExpensesAsync(careGroupId, request.SplitMode, request.ExpenseIds, request.TargetDate);
            if (validExpenses.Count == 0)
            {
                throw new DomainException("查無可結算的待分帳支出", "BAD_REQUEST", 400);
            }

            var targetUserIds = (request.TargetUserIds ?? new List<Guid>()).Distinct().ToList();
            if (targetUserIds.Count == 0)
            {
                throw new DomainException("請至少提供一位分帳成員", "BAD_REQUEST", 400);
            }

            var now = GetUtcNowRoundedToMilliseconds();
            var createdBy = currentUserId.ToString();
            var batchId = Guid.NewGuid();

            foreach (var expense in validExpenses)
            {
                expense.SplitStatus = ExpenseSplitStatus.Settled;
                expense.UpdatedAt = now;
                await _expenseRepository.UpdateAsync(expense);

                Guid? payerId = Guid.TryParse(expense.CreatedBy, out var parsedPayer) ? parsedPayer : null;
                var splits = BuildSplitsForExpense(expense, targetUserIds, payerId, createdBy, now, batchId);
                await _expenseSplitRepository.AddRangeAsync(splits);
            }

            await _expenseRepository.SaveChangesAsync();
            await _expenseSplitRepository.SaveChangesAsync();

            return new SplitConfirmResponse
            {
                SplitBatchId = batchId,
                ExpenseCount = validExpenses.Count,
                TotalAmount = validExpenses.Sum(expense => expense.Amount)
            };
        }

        public async Task<MemberExpenseTotalsResponse> GetMemberExpenseTotalsAsync(Guid currentUserId, Guid careGroupId, MemberExpenseTotalsRequest request)
        {
            await CheckMembershipAsync(careGroupId, currentUserId);

            var (dateFromUtc, dateToUtc) = ResolveScopeRange(request.Scope, request.TargetDate);
            var members = await _careGroupRepository.GetActiveMembersWithUserAsync(careGroupId);

            var payerTotalsByUser = await BuildPayerTotalsAsync(careGroupId, dateFromUtc, dateToUtc);
            var shareTotalsByUser = await BuildShareTotalsAsync(careGroupId, dateFromUtc, dateToUtc);
            var selfExpenseTotalsByUser = await BuildSelfExpenseTotalsAsync(careGroupId, dateFromUtc, dateToUtc);

            var items = members
                .Where(member => member.User != null)
                .Select(member =>
                {
                    var payerStat = payerTotalsByUser.TryGetValue(member.UserId, out var payer) ? payer : (Total: 0m, Count: 0);
                    var shareStat = shareTotalsByUser.TryGetValue(member.UserId, out var share) ? share : (Total: 0m, Count: 0);
                    var selfExpenseStat = selfExpenseTotalsByUser.TryGetValue(member.UserId, out var selfExpense) ? selfExpense : (Total: 0m, Count: 0);

                    return new MemberExpenseTotalItem
                    {
                        UserId = member.UserId,
                        Name = member.User!.Username,
                        AvatarUrl = string.IsNullOrWhiteSpace(member.User.AvatarKey) ? null : member.User.AvatarKey,
                        PayerTotal = payerStat.Total,
                        PayerCount = payerStat.Count,
                        ShareTotal = shareStat.Total,
                        ShareCount = shareStat.Count,
                        SelfExpenseTotal = selfExpenseStat.Total,
                        SelfExpenseCount = selfExpenseStat.Count,
                        PersonalPayableTotal = shareStat.Total + selfExpenseStat.Total,
                        CurrentPayableTotal = payerStat.Total + shareStat.Total + selfExpenseStat.Total
                    };
                })
                .OrderByDescending(item => item.CurrentPayableTotal)
                .ThenBy(item => item.Name)
                .ToList();

            return new MemberExpenseTotalsResponse
            {
                MemberCount = items.Count,
                PayerTotalAmount = items.Sum(item => item.PayerTotal),
                ShareTotalAmount = items.Sum(item => item.ShareTotal),
                SelfExpenseTotalAmount = items.Sum(item => item.SelfExpenseTotal),
                PersonalPayableTotalAmount = items.Sum(item => item.PersonalPayableTotal),
                CurrentPayableTotalAmount = items.Sum(item => item.CurrentPayableTotal),
                Members = items
            };
        }

        private async Task CheckMembershipAsync(Guid careGroupId, Guid userId)
        {
            var isMember = await _careGroupRepository.IsMemberAsync(careGroupId, userId);
            if (!isMember)
            {
                throw new DomainException("您不是此 Care Group 的成員", "FORBIDDEN", 403);
            }
        }

        private async Task<ExpenseSplitPreviewResponse> BuildSplitPreviewAsync(
            Guid careGroupId,
            string splitMode,
            List<Guid>? expenseIds,
            DateTime? targetDate,
            IReadOnlyCollection<User> targetUsers)
        {
            var participants = targetUsers
                .GroupBy(user => user.Id)
                .Select(group => group.First())
                .ToList();

            if (participants.Count == 0)
            {
                throw new DomainException("請至少提供一位分帳成員", "BAD_REQUEST", 400);
            }

            var validExpenses = await ResolveExpensesAsync(careGroupId, splitMode, expenseIds, targetDate);
            if (validExpenses.Count == 0)
            {
                throw new DomainException("查無可預覽的待分帳支出", "BAD_REQUEST", 400);
            }

            var totalAmount = validExpenses.Sum(expense => expense.Amount);
            var sharePerPerson = Math.Round(totalAmount / participants.Count, 2);
            var paidAmounts = participants.ToDictionary(user => user.Id, _ => 0m);

            foreach (var expense in validExpenses)
            {
                if (Guid.TryParse(expense.CreatedBy, out var payerId) && paidAmounts.ContainsKey(payerId))
                {
                    paidAmounts[payerId] += expense.Amount;
                }
            }

            var splitDetails = participants.Select(user =>
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
                ExpenseCount = validExpenses.Count,
                TotalAmount = totalAmount,
                SelectedExpenses = validExpenses.Select(expense => new ExpenseItemSummary
                {
                    Id = expense.Id,
                    Title = expense.Title,
                    Amount = expense.Amount
                }).ToList(),
                SplitDetails = splitDetails
            };
        }

        private static List<ExpenseSplit> BuildSplitsForExpense(
            ExpenseRecord expense,
            List<Guid> targetUserIds,
            Guid? payerId,
            string createdBy,
            DateTime now,
            Guid splitBatchId)
        {
            var count = targetUserIds.Count;
            var baseShare = Math.Round(expense.Amount / count, 2, MidpointRounding.AwayFromZero);
            var splits = new List<ExpenseSplit>(count);

            for (int i = 0; i < count; i++)
            {
                var userId = targetUserIds[i];
                var share = i == count - 1
                    ? expense.Amount - baseShare * (count - 1)
                    : baseShare;

                splits.Add(new ExpenseSplit
                {
                    ExpenseId = expense.Id,
                    Expense = expense,
                    UserId = userId,
                    ShareAmount = share,
                    IsPayer = payerId.HasValue && payerId.Value == userId,
                    CareGroupId = expense.CareGroupId,
                    SplitBatchId = splitBatchId,
                    CreatedAt = now,
                    UpdatedAt = now,
                    CreatedBy = createdBy
                });
            }

            return splits;
        }

        private async Task<ExpenseSplitPreviewResponse> BuildSettledSplitResultAsync(Guid careGroupId, Guid splitBatchId)
        {
            var splits = await _expenseSplitRepository.GetByBatchIdAsync(careGroupId, splitBatchId);
            if (splits.Count == 0)
            {
                throw new DomainException("查無此分帳批次", "NOT_FOUND", 404);
            }

            var expenseSummaries = splits
                .Where(split => split.Expense != null)
                .GroupBy(split => split.ExpenseId)
                .Select(group =>
                {
                    var expense = group.First().Expense!;
                    return new ExpenseItemSummary
                    {
                        Id = expense.Id,
                        Title = expense.Title,
                        Amount = expense.Amount
                    };
                })
                .ToList();

            // 一次分帳對所有 expense 的參與成員一致，取任一筆 expense 的 splits 即可推每人應分攤的金額。
            var perUserShare = splits
                .GroupBy(split => split.UserId)
                .ToDictionary(group => group.Key, group => group.Sum(split => split.ShareAmount));

            // 付款金額：以該批次內，每個 expense 的 CreatedBy 為付款人來累加。
            var paidAmounts = new Dictionary<Guid, decimal>();
            foreach (var summary in expenseSummaries)
            {
                var expense = splits.First(split => split.ExpenseId == summary.Id).Expense!;
                if (Guid.TryParse(expense.CreatedBy, out var payerId))
                {
                    paidAmounts.TryGetValue(payerId, out var current);
                    paidAmounts[payerId] = current + expense.Amount;
                }
            }

            var participantIds = perUserShare.Keys.ToList();
            var users = (await _userRepository.GetListByIdsAsync(participantIds))
                .ToDictionary(user => user.Id);

            var splitDetails = participantIds.Select(userId =>
            {
                users.TryGetValue(userId, out var user);
                var paid = paidAmounts.GetValueOrDefault(userId, 0m);
                var share = perUserShare[userId];
                var balance = paid - share;

                return new SplitUserDetail
                {
                    UserId = userId,
                    Name = user?.Username ?? string.Empty,
                    AvatarUrl = user?.AvatarKey,
                    IsPayer = paid > 0,
                    ReceivableAmount = balance > 0 ? balance : 0,
                    PayableAmount = balance < 0 ? Math.Abs(balance) : 0
                };
            }).ToList();

            // ExecutedBy / ExecutedAt 取自 splits 中任一筆（同批次共用）。
            var anchor = splits[0];
            SplitExecutor? executor = null;
            if (Guid.TryParse(anchor.CreatedBy, out var executorId))
            {
                var executorUser = await _userRepository.GetByIdAsync(executorId);
                if (executorUser != null)
                {
                    executor = new SplitExecutor
                    {
                        UserId = executorUser.Id,
                        Name = executorUser.Username,
                        AvatarUrl = executorUser.AvatarKey
                    };
                }
            }

            return new ExpenseSplitPreviewResponse
            {
                ExpenseCount = expenseSummaries.Count,
                TotalAmount = expenseSummaries.Sum(item => item.Amount),
                SelectedExpenses = expenseSummaries,
                SplitDetails = splitDetails,
                ExecutedBy = executor,
                ExecutedAt = anchor.CreatedAt
            };
        }

        private async Task<List<ExpenseRecord>> ResolveExpensesAsync(Guid careGroupId, string splitMode, List<Guid>? expenseIds, DateTime? targetDate)
        {
            var mode = (splitMode ?? "custom").Trim().ToLowerInvariant();

            if (mode == "daily")
            {
                var anchorTaiwanDate = ResolveTaiwanAnchorDate(targetDate);
                var dayStartTaiwan = new DateTime(anchorTaiwanDate.Year, anchorTaiwanDate.Month, anchorTaiwanDate.Day, 0, 0, 0, DateTimeKind.Unspecified);
                var dayEndTaiwan = dayStartTaiwan.AddDays(1);
                return await _expenseRepository.GetPendingByDateRangeAsync(
                    careGroupId,
                    TimeHelper.TaiwanToUtc(dayStartTaiwan),
                    TimeHelper.TaiwanToUtc(dayEndTaiwan));
            }

            if (mode == "monthly")
            {
                var anchorTaiwanDate = ResolveTaiwanAnchorDate(targetDate);
                var monthStartTaiwan = new DateTime(anchorTaiwanDate.Year, anchorTaiwanDate.Month, 1, 0, 0, 0, DateTimeKind.Unspecified);
                var monthEndTaiwan = monthStartTaiwan.AddMonths(1);
                return await _expenseRepository.GetPendingByDateRangeAsync(
                    careGroupId,
                    TimeHelper.TaiwanToUtc(monthStartTaiwan),
                    TimeHelper.TaiwanToUtc(monthEndTaiwan));
            }

            if (expenseIds == null || expenseIds.Count == 0)
            {
                throw new DomainException("custom 模式請提供 expenseIds", "BAD_REQUEST", 400);
            }

            var expenses = await _expenseRepository.GetListByIdsAsync(careGroupId, expenseIds);
            return expenses.Where(expense => expense.SplitStatus == ExpenseSplitStatus.Pending).ToList();
        }

        private async Task<Dictionary<Guid, (decimal Total, int Count)>> BuildPayerTotalsAsync(
            Guid careGroupId,
            DateTime? dateFromUtc,
            DateTime? dateToUtc)
        {
            var expenses = await _expenseRepository.GetByCareGroupAsync(careGroupId, dateFromUtc, dateToUtc, ExpenseSplitStatus.Pending);

            return expenses
                .Where(expense => Guid.TryParse(expense.CreatedBy, out _))
                .GroupBy(expense => Guid.Parse(expense.CreatedBy))
                .ToDictionary(group => group.Key, group => (Total: group.Sum(item => item.Amount), Count: group.Count()));
        }

        private async Task<Dictionary<Guid, (decimal Total, int Count)>> BuildShareTotalsAsync(
            Guid careGroupId,
            DateTime? dateFromUtc,
            DateTime? dateToUtc)
        {
            var splits = await _expenseSplitRepository.GetByCareGroupAsync(careGroupId, dateFromUtc, dateToUtc);

            return splits
                .GroupBy(split => split.UserId)
                .ToDictionary(group => group.Key, group => (Total: group.Sum(item => item.ShareAmount), Count: group.Count()));
        }

        private async Task<Dictionary<Guid, (decimal Total, int Count)>> BuildSelfExpenseTotalsAsync(
            Guid careGroupId,
            DateTime? dateFromUtc,
            DateTime? dateToUtc)
        {
            var expenses = await _expenseRepository.GetByCareGroupAsync(careGroupId, dateFromUtc, dateToUtc, ExpenseSplitStatus.None);

            return expenses
                .Where(expense => Guid.TryParse(expense.CreatedBy, out _))
                .GroupBy(expense => Guid.Parse(expense.CreatedBy))
                .ToDictionary(group => group.Key, group => (Total: group.Sum(item => item.Amount), Count: group.Count()));
        }

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

            var monthStartTaiwan = new DateTime(anchorTaiwanDate.Year, anchorTaiwanDate.Month, 1, 0, 0, 0, DateTimeKind.Unspecified);
            var monthEndTaiwan = monthStartTaiwan.AddMonths(1);
            return (TimeHelper.TaiwanToUtc(monthStartTaiwan), TimeHelper.TaiwanToUtc(monthEndTaiwan));
        }

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

            return value.Date;
        }

        private static ExpenseResponse MapToResponse(ExpenseRecord expense, Guid? splitBatchId = null)
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
                CreatedBy = expense.CreatedBy,
                SplitBatchId = splitBatchId
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
