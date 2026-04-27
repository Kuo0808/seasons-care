using SeasonsCare.Api.DTOs.Common;
using SeasonsCare.Api.DTOs.Expenses;
using SeasonsCare.Api.Exceptions;
using SeasonsCare.Api.Models.Entities;
using SeasonsCare.Api.Models.Enums;
using SeasonsCare.Api.Repositories;
using SeasonsCare.Api.Services;

namespace SeasonsCare.Api.Tests.Services;

public class ExpenseServiceTests
{
    [Fact]
    public async Task CreateExpenseAsync_ThrowsForbidden_WhenUserIsNotMember()
    {
        var repository = new FakeExpenseRepository();
        var groupRepository = new FakeCareGroupRepository(isMember: false);
        var userRepository = new FakeUserRepository();
        var splitRepository = new FakeExpenseSplitRepository();
        var service = new ExpenseService(repository, groupRepository, userRepository, splitRepository);

        var exception = await Assert.ThrowsAsync<DomainException>(() =>
            service.CreateExpenseAsync(Guid.NewGuid(), Guid.NewGuid(), new CreateExpenseRequest
            {
                Title = "Taxi",
                Amount = 120m
            }));

        Assert.Equal(403, exception.StatusCode);
        Assert.Equal("FORBIDDEN", exception.ErrorCode);
    }

    [Fact]
    public async Task CreateExpenseAsync_CreatesExpense_WithInitialConcurrencyTimestamp()
    {
        var repository = new FakeExpenseRepository();
        var groupRepository = new FakeCareGroupRepository(isMember: true);
        var userRepository = new FakeUserRepository();
        var splitRepository = new FakeExpenseSplitRepository();
        var service = new ExpenseService(repository, groupRepository, userRepository, splitRepository);
        var userId = Guid.NewGuid();
        var careGroupId = Guid.NewGuid();

        var result = await service.CreateExpenseAsync(userId, careGroupId, new CreateExpenseRequest
        {
            Title = "Taxi",
            Amount = 250m,
            Category = "traffic",
            ExpenseDate = new DateTime(2026, 3, 20, 2, 0, 0, DateTimeKind.Utc),
            SplitStatus = ExpenseSplitStatus.Pending
        });

        Assert.NotEqual(Guid.Empty, result.Id);
        Assert.Equal(careGroupId, result.CareGroupId);
        Assert.Equal(userId.ToString(), result.CreatedBy);
        Assert.Equal(result.CreatedAt, result.UpdatedAt);
        Assert.Equal(ExpenseSplitStatus.Pending, result.SplitStatus);
        Assert.Single(repository.Expenses);
    }

    [Fact]
    public async Task UpdateExpenseAsync_ThrowsConflict_WhenUpdatedAtIsMissing()
    {
        var existing = new ExpenseRecord
        {
            Id = Guid.NewGuid(),
            CareGroupId = Guid.NewGuid(),
            Title = "Taxi",
            Amount = 100m,
            Category = "traffic",
            ExpenseDate = new DateTime(2026, 3, 20, 2, 0, 0, DateTimeKind.Utc),
            SplitStatus = ExpenseSplitStatus.None,
            UpdatedAt = DateTime.UtcNow
        };

        var repository = new FakeExpenseRepository(existing);
        var groupRepository = new FakeCareGroupRepository(isMember: true);
        var userRepository = new FakeUserRepository();
        var splitRepository = new FakeExpenseSplitRepository();
        var service = new ExpenseService(repository, groupRepository, userRepository, splitRepository);

        var exception = await Assert.ThrowsAsync<DomainException>(() =>
            service.UpdateExpenseAsync(Guid.NewGuid(), existing.CareGroupId, existing.Id, new UpdateExpenseRequest
            {
                Title = "Taxi 2",
                Amount = 120m,
                Category = "traffic",
                ExpenseDate = existing.ExpenseDate,
                SplitStatus = ExpenseSplitStatus.None
            }));

        Assert.Equal(409, exception.StatusCode);
        Assert.Equal("CONCURRENCY_CONFLICT", exception.ErrorCode);
    }

    [Fact]
    public async Task UpdateExpenseAsync_ThrowsConflict_WhenUpdatedAtDoesNotMatch()
    {
        var existing = new ExpenseRecord
        {
            Id = Guid.NewGuid(),
            CareGroupId = Guid.NewGuid(),
            Title = "Taxi",
            Amount = 100m,
            Category = "traffic",
            ExpenseDate = new DateTime(2026, 3, 20, 2, 0, 0, DateTimeKind.Utc),
            SplitStatus = ExpenseSplitStatus.None,
            UpdatedAt = new DateTime(2026, 3, 20, 2, 0, 0, DateTimeKind.Utc)
        };

        var repository = new FakeExpenseRepository(existing);
        var groupRepository = new FakeCareGroupRepository(isMember: true);
        var userRepository = new FakeUserRepository();
        var splitRepository = new FakeExpenseSplitRepository();
        var service = new ExpenseService(repository, groupRepository, userRepository, splitRepository);

        var exception = await Assert.ThrowsAsync<DomainException>(() =>
            service.UpdateExpenseAsync(Guid.NewGuid(), existing.CareGroupId, existing.Id, new UpdateExpenseRequest
            {
                Title = "Taxi 2",
                Amount = 120m,
                Category = "traffic",
                ExpenseDate = existing.ExpenseDate,
                SplitStatus = ExpenseSplitStatus.None,
                UpdatedAt = existing.UpdatedAt.Value.AddSeconds(-1)
            }));

        Assert.Equal(409, exception.StatusCode);
        Assert.Equal("CONCURRENCY_CONFLICT", exception.ErrorCode);
    }

    [Fact]
    public async Task UpdateExpenseAsync_UpdatesExpense_WhenUpdatedAtMatches()
    {
        var existingUpdatedAt = new DateTime(2026, 3, 20, 2, 0, 0, 123, DateTimeKind.Utc);
        var existing = new ExpenseRecord
        {
            Id = Guid.NewGuid(),
            CareGroupId = Guid.NewGuid(),
            Title = "Taxi",
            Amount = 100m,
            Category = "traffic",
            Notes = "Old note",
            ExpenseDate = new DateTime(2026, 3, 20, 1, 0, 0, DateTimeKind.Utc),
            SplitStatus = ExpenseSplitStatus.Pending,
            UpdatedAt = existingUpdatedAt
        };

        var repository = new FakeExpenseRepository(existing);
        var groupRepository = new FakeCareGroupRepository(isMember: true);
        var userRepository = new FakeUserRepository();
        var splitRepository = new FakeExpenseSplitRepository();
        var service = new ExpenseService(repository, groupRepository, userRepository, splitRepository);

        var result = await service.UpdateExpenseAsync(Guid.NewGuid(), existing.CareGroupId, existing.Id, new UpdateExpenseRequest
        {
            Title = "Groceries",
            Amount = 320m,
            Category = "food",
            Notes = "New note",
            ExpenseDate = existing.ExpenseDate.AddHours(1),
            SplitStatus = ExpenseSplitStatus.Settled,
            UpdatedAt = existingUpdatedAt
        });

        Assert.Equal("Groceries", result.Title);
        Assert.Equal(320m, result.Amount);
        Assert.Equal("food", result.Category);
        Assert.Equal("New note", result.Notes);
        Assert.Equal(ExpenseSplitStatus.Settled, result.SplitStatus);
        Assert.NotEqual(existingUpdatedAt, result.UpdatedAt);
    }

    [Fact]
    public async Task PreviewSplitAsync_CalculatesCorrectReceivableAndPayable()
    {
        var payerId = Guid.NewGuid();
        var anotherPayerId = Guid.NewGuid();
        var nonPayerId = Guid.NewGuid();

        var existingExpenses = new[]
        {
            new ExpenseRecord { Id = Guid.NewGuid(), CareGroupId = Guid.NewGuid(), Title = "A", Amount = 1000m, SplitStatus = ExpenseSplitStatus.Pending, CreatedBy = payerId.ToString() },
            new ExpenseRecord { Id = Guid.NewGuid(), CareGroupId = Guid.NewGuid(), Title = "B", Amount = 200m, SplitStatus = ExpenseSplitStatus.Pending, CreatedBy = anotherPayerId.ToString() }
        };

        var users = new[]
        {
            new User { Id = payerId, Username = "Payer1" },
            new User { Id = anotherPayerId, Username = "Payer2" },
            new User { Id = nonPayerId, Username = "NonPayer" }
        };

        var careGroupId = existingExpenses[0].CareGroupId;
        existingExpenses[1].CareGroupId = careGroupId;

        var repository = new FakeExpenseRepository(existingExpenses);
        var groupRepository = new FakeCareGroupRepository(isMember: true);
        var userRepository = new FakeUserRepository(users);
        var splitRepository = new FakeExpenseSplitRepository();
        var service = new ExpenseService(repository, groupRepository, userRepository, splitRepository);

        var result = await service.PreviewSplitAsync(payerId, careGroupId, new SplitPreviewRequest
        {
            SplitMode = "custom",
            ExpenseIds = new List<Guid> { existingExpenses[0].Id, existingExpenses[1].Id },
            TargetUserIds = new List<Guid> { payerId, anotherPayerId, nonPayerId } // 3 users
        });

        Assert.Equal(1200m, result.TotalAmount); // 1000 + 200
        Assert.Equal(2, result.SelectedExpenses.Count);
        Assert.Equal(3, result.SplitDetails.Count);

        var p1 = result.SplitDetails.First(x => x.UserId == payerId);
        Assert.True(p1.IsPayer);
        Assert.Equal(1000m - 400m, p1.ReceivableAmount); // 600
        Assert.Equal(0m, p1.PayableAmount);

        var p2 = result.SplitDetails.First(x => x.UserId == anotherPayerId);
        Assert.True(p2.IsPayer);
        Assert.Equal(0m, p2.ReceivableAmount); 
        Assert.Equal(400m - 200m, p2.PayableAmount); // 200 (since 200 paid - 400 share = -200)

        var p3 = result.SplitDetails.First(x => x.UserId == nonPayerId);
        Assert.False(p3.IsPayer);
        Assert.Equal(0m, p3.ReceivableAmount);
        Assert.Equal(400m, p3.PayableAmount); // 400
    }

    [Fact]
    public async Task GetExpensesAsync_PopulatesSplitBatchId_ForSettledExpensesOnly()
    {
        var careGroupId = Guid.NewGuid();
        var settledExpenseId = Guid.NewGuid();
        var pendingExpenseId = Guid.NewGuid();
        var batchId = Guid.NewGuid();

        var settled = new ExpenseRecord
        {
            Id = settledExpenseId,
            CareGroupId = careGroupId,
            Title = "已分帳",
            Amount = 3000m,
            SplitStatus = ExpenseSplitStatus.Settled,
            CreatedBy = Guid.NewGuid().ToString(),
            ExpenseDate = DateTime.UtcNow
        };
        var pending = new ExpenseRecord
        {
            Id = pendingExpenseId,
            CareGroupId = careGroupId,
            Title = "待分帳",
            Amount = 500m,
            SplitStatus = ExpenseSplitStatus.Pending,
            CreatedBy = Guid.NewGuid().ToString(),
            ExpenseDate = DateTime.UtcNow
        };

        var repository = new FakeExpenseRepository(settled, pending);
        var groupRepository = new FakeCareGroupRepository(isMember: true);
        var userRepository = new FakeUserRepository();
        var splitRepository = new FakeExpenseSplitRepository();
        // 模擬 settled expense 的分帳明細：不同成員的 row 都共用同一個 batchId
        splitRepository.Splits.Add(new ExpenseSplit
        {
            CareGroupId = careGroupId,
            ExpenseId = settledExpenseId,
            SplitBatchId = batchId,
            UserId = Guid.NewGuid(),
            ShareAmount = 1500m
        });
        splitRepository.Splits.Add(new ExpenseSplit
        {
            CareGroupId = careGroupId,
            ExpenseId = settledExpenseId,
            SplitBatchId = batchId,
            UserId = Guid.NewGuid(),
            ShareAmount = 1500m
        });

        var service = new ExpenseService(repository, groupRepository, userRepository, splitRepository);

        var result = await service.GetExpensesAsync(Guid.NewGuid(), careGroupId, new PaginationRequest());

        var settledItem = result.Items.Single(x => x.Id == settledExpenseId);
        var pendingItem = result.Items.Single(x => x.Id == pendingExpenseId);

        Assert.Equal(batchId, settledItem.SplitBatchId);
        Assert.Null(pendingItem.SplitBatchId);
    }

    [Fact]
    public async Task GetExpenseByIdAsync_ReturnsSplitBatchId_WhenSettled()
    {
        var careGroupId = Guid.NewGuid();
        var expenseId = Guid.NewGuid();
        var batchId = Guid.NewGuid();

        var settled = new ExpenseRecord
        {
            Id = expenseId,
            CareGroupId = careGroupId,
            Title = "已分帳",
            Amount = 3000m,
            SplitStatus = ExpenseSplitStatus.Settled,
            CreatedBy = Guid.NewGuid().ToString(),
            ExpenseDate = DateTime.UtcNow
        };

        var repository = new FakeExpenseRepository(settled);
        var groupRepository = new FakeCareGroupRepository(isMember: true);
        var userRepository = new FakeUserRepository();
        var splitRepository = new FakeExpenseSplitRepository();
        splitRepository.Splits.Add(new ExpenseSplit
        {
            CareGroupId = careGroupId,
            ExpenseId = expenseId,
            SplitBatchId = batchId,
            UserId = Guid.NewGuid(),
            ShareAmount = 3000m
        });

        var service = new ExpenseService(repository, groupRepository, userRepository, splitRepository);

        var result = await service.GetExpenseByIdAsync(Guid.NewGuid(), careGroupId, expenseId);

        Assert.Equal(batchId, result.SplitBatchId);
    }

    [Fact]
    public async Task GetSplitPreviewAsync_UsesActiveCareGroupMembers_AsParticipants()
    {
        var careGroupId = Guid.NewGuid();
        var payerId = Guid.NewGuid();
        var memberId = Guid.NewGuid();

        var expenses = new[]
        {
            new ExpenseRecord
            {
                Id = Guid.NewGuid(),
                CareGroupId = careGroupId,
                Title = "回診費",
                Amount = 1200m,
                SplitStatus = ExpenseSplitStatus.Pending,
                CreatedBy = payerId.ToString(),
                ExpenseDate = DateTime.UtcNow
            }
        };

        var users = new[]
        {
            new User { Id = payerId, Username = "王希銘", AvatarKey = "payer.png" },
            new User { Id = memberId, Username = "王小美", AvatarKey = "member.png" }
        };

        var careGroupRepository = new FakeCareGroupRepository(isMember: true)
        {
            Members = new List<CareGroupMember>
            {
                new CareGroupMember { CareGroupId = careGroupId, UserId = payerId, User = users[0] },
                new CareGroupMember { CareGroupId = careGroupId, UserId = memberId, User = users[1] }
            }
        };

        var repository = new FakeExpenseRepository(expenses);
        var userRepository = new FakeUserRepository(users);
        var splitRepository = new FakeExpenseSplitRepository();
        var service = new ExpenseService(repository, careGroupRepository, userRepository, splitRepository);

        var result = await service.GetSplitPreviewAsync(payerId, careGroupId, new SplitPreviewQueryRequest
        {
            SplitMode = "custom",
            ExpenseIds = new List<Guid> { expenses[0].Id }
        });

        Assert.Equal(1, result.ExpenseCount);
        Assert.Equal(1200m, result.TotalAmount);
        Assert.Single(result.SelectedExpenses);
        Assert.Equal(2, result.SplitDetails.Count);

        var payer = result.SplitDetails.First(x => x.UserId == payerId);
        Assert.True(payer.IsPayer);
        Assert.Equal(600m, payer.ReceivableAmount);
        Assert.Equal(0m, payer.PayableAmount);

        var member = result.SplitDetails.First(x => x.UserId == memberId);
        Assert.False(member.IsPayer);
        Assert.Equal(0m, member.ReceivableAmount);
        Assert.Equal(600m, member.PayableAmount);
    }

    [Fact]
    public async Task GetMemberExpenseTotalsAsync_AggregatesByPayer_AndIncludesNonPayingMembers()
    {
        var careGroupId = Guid.NewGuid();
        var payerId = Guid.NewGuid();
        var nonPayerId = Guid.NewGuid();

        var expenses = new[]
        {
            new ExpenseRecord { Id = Guid.NewGuid(), CareGroupId = careGroupId, Title = "A", Amount = 1000m, SplitStatus = ExpenseSplitStatus.Pending, CreatedBy = payerId.ToString(), ExpenseDate = DateTime.UtcNow },
            new ExpenseRecord { Id = Guid.NewGuid(), CareGroupId = careGroupId, Title = "B", Amount = 200m, SplitStatus = ExpenseSplitStatus.Pending, CreatedBy = payerId.ToString(), ExpenseDate = DateTime.UtcNow },
            // Settled 不應被計入（pendingOnly = true）
            new ExpenseRecord { Id = Guid.NewGuid(), CareGroupId = careGroupId, Title = "C", Amount = 999m, SplitStatus = ExpenseSplitStatus.Settled, CreatedBy = payerId.ToString(), ExpenseDate = DateTime.UtcNow }
        };

        var users = new[]
        {
            new User { Id = payerId, Username = "Payer", AvatarKey = "avatar/payer.png" },
            new User { Id = nonPayerId, Username = "NonPayer" }
        };

        var careGroupRepository = new FakeCareGroupRepository(isMember: true)
        {
            Members = new List<CareGroupMember>
            {
                new CareGroupMember { CareGroupId = careGroupId, UserId = payerId, User = users[0] },
                new CareGroupMember { CareGroupId = careGroupId, UserId = nonPayerId, User = users[1] }
            }
        };

        var repository = new FakeExpenseRepository(expenses);
        var userRepository = new FakeUserRepository(users);
        var splitRepository = new FakeExpenseSplitRepository();
        var service = new ExpenseService(repository, careGroupRepository, userRepository, splitRepository);

        var result = await service.GetMemberExpenseTotalsAsync(payerId, careGroupId, new MemberExpenseTotalsRequest
        {
            Scope = "all"
        });

        // payer 視角只算 Pending，Settled 那筆 999 不該被列入
        Assert.Equal(1200m, result.PayerTotalAmount);
        Assert.Equal(0m, result.ShareTotalAmount); // 沒有分帳明細
        Assert.Equal(2, result.MemberCount);

        var payerItem = result.Members.First(x => x.UserId == payerId);
        Assert.Equal(1200m, payerItem.PayerTotal);
        Assert.Equal(2, payerItem.PayerCount);
        Assert.Equal(0m, payerItem.ShareTotal);
        Assert.Equal(0, payerItem.ShareCount);
        Assert.Equal("avatar/payer.png", payerItem.AvatarUrl);

        var nonPayerItem = result.Members.First(x => x.UserId == nonPayerId);
        Assert.Equal(0m, nonPayerItem.PayerTotal);
        Assert.Equal(0, nonPayerItem.PayerCount);
        Assert.Equal(0m, nonPayerItem.ShareTotal);
        Assert.Equal(0, nonPayerItem.ShareCount);
        Assert.Null(nonPayerItem.AvatarUrl);

        // 排序：payerTotal + shareTotal 由高到低
        Assert.Equal(payerId, result.Members[0].UserId);
        Assert.Equal(nonPayerId, result.Members[1].UserId);
    }

    [Fact]
    public async Task ConfirmSplitAsync_WritesExpenseSplits_AndMarksExpensesSettled()
    {
        var careGroupId = Guid.NewGuid();
        var payerId = Guid.NewGuid();
        var memberA = Guid.NewGuid();
        var memberB = Guid.NewGuid();

        var existing = new ExpenseRecord
        {
            Id = Guid.NewGuid(),
            CareGroupId = careGroupId,
            Title = "藥局自費",
            Amount = 999m, // 故意取奇數驗證 round 尾差吸收
            SplitStatus = ExpenseSplitStatus.Pending,
            CreatedBy = payerId.ToString(),
            ExpenseDate = DateTime.UtcNow
        };

        var repository = new FakeExpenseRepository(existing);
        var groupRepository = new FakeCareGroupRepository(isMember: true);
        var userRepository = new FakeUserRepository();
        var splitRepository = new FakeExpenseSplitRepository();
        var service = new ExpenseService(repository, groupRepository, userRepository, splitRepository);

        var confirmResult = await service.ConfirmSplitAsync(payerId, careGroupId, new SplitConfirmRequest
        {
            SplitMode = "custom",
            ExpenseIds = new List<Guid> { existing.Id },
            TargetUserIds = new List<Guid> { payerId, memberA, memberB }
        });

        Assert.Equal(ExpenseSplitStatus.Settled, existing.SplitStatus);

        Assert.NotEqual(Guid.Empty, confirmResult.SplitBatchId);
        Assert.Equal(1, confirmResult.ExpenseCount);
        Assert.Equal(999m, confirmResult.TotalAmount);

        Assert.Equal(3, splitRepository.Splits.Count);
        // 三人平均 999 → 333.00 / 333.00 / 333.00（999 / 3 剛好整除，但驗證加總 = 原金額）
        Assert.Equal(999m, splitRepository.Splits.Sum(s => s.ShareAmount));
        Assert.All(splitRepository.Splits, s => Assert.Equal(existing.Id, s.ExpenseId));
        Assert.All(splitRepository.Splits, s => Assert.Equal(careGroupId, s.CareGroupId));
        // 同一次 confirm 寫入的所有 split 共用同一個 batchId，且等於回傳的 SplitBatchId
        Assert.All(splitRepository.Splits, s => Assert.Equal(confirmResult.SplitBatchId, s.SplitBatchId));

        // 只有付款人會被標 IsPayer
        Assert.True(splitRepository.Splits.Single(s => s.UserId == payerId).IsPayer);
        Assert.False(splitRepository.Splits.Single(s => s.UserId == memberA).IsPayer);
        Assert.False(splitRepository.Splits.Single(s => s.UserId == memberB).IsPayer);
    }

    [Fact]
    public async Task ConfirmSplitAsync_AbsorbsRoundingDiff_OnLastSplit()
    {
        var careGroupId = Guid.NewGuid();
        var payerId = Guid.NewGuid();
        var memberA = Guid.NewGuid();
        var memberB = Guid.NewGuid();

        var existing = new ExpenseRecord
        {
            Id = Guid.NewGuid(),
            CareGroupId = careGroupId,
            Title = "三人分 100 元",
            Amount = 100m, // 100 / 3 = 33.33...，最後一位要吸收尾差
            SplitStatus = ExpenseSplitStatus.Pending,
            CreatedBy = payerId.ToString(),
            ExpenseDate = DateTime.UtcNow
        };

        var repository = new FakeExpenseRepository(existing);
        var groupRepository = new FakeCareGroupRepository(isMember: true);
        var userRepository = new FakeUserRepository();
        var splitRepository = new FakeExpenseSplitRepository();
        var service = new ExpenseService(repository, groupRepository, userRepository, splitRepository);

        await service.ConfirmSplitAsync(payerId, careGroupId, new SplitConfirmRequest
        {
            SplitMode = "custom",
            ExpenseIds = new List<Guid> { existing.Id },
            TargetUserIds = new List<Guid> { payerId, memberA, memberB }
        });

        // sum 必須剛好等於原金額
        Assert.Equal(100m, splitRepository.Splits.Sum(s => s.ShareAmount));
    }

    [Fact]
    public async Task GetSplitPreviewAsync_WithSplitBatchId_ReturnsSettledResult_WithExecutorAndPayables()
    {
        var careGroupId = Guid.NewGuid();
        var ivy = new User { Id = Guid.NewGuid(), Username = "艾薇", AvatarKey = "avatar/ivy.png" };
        var awei = new User { Id = Guid.NewGuid(), Username = "阿偉" };
        var ahe = new User { Id = Guid.NewGuid(), Username = "阿和" };

        var expense = new ExpenseRecord
        {
            Id = Guid.NewGuid(),
            CareGroupId = careGroupId,
            Title = "自費藥品",
            Amount = 3000m,
            SplitStatus = ExpenseSplitStatus.Pending,
            CreatedBy = ivy.Id.ToString(),
            ExpenseDate = DateTime.UtcNow
        };

        var repository = new FakeExpenseRepository(expense);
        var groupRepository = new FakeCareGroupRepository(isMember: true);
        var splitRepository = new FakeExpenseSplitRepository();
        var userRepository = new FakeUserRepository(ivy, awei, ahe);
        var service = new ExpenseService(repository, groupRepository, userRepository, splitRepository);

        var confirmResult = await service.ConfirmSplitAsync(ivy.Id, careGroupId, new SplitConfirmRequest
        {
            SplitMode = "custom",
            ExpenseIds = new List<Guid> { expense.Id },
            TargetUserIds = new List<Guid> { ivy.Id, awei.Id, ahe.Id }
        });

        var result = await service.GetSplitPreviewAsync(ivy.Id, careGroupId, new SplitPreviewQueryRequest
        {
            SplitBatchId = confirmResult.SplitBatchId
        });

        Assert.Equal(1, result.ExpenseCount);
        Assert.Equal(3000m, result.TotalAmount);
        Assert.Single(result.SelectedExpenses);
        Assert.Equal("自費藥品", result.SelectedExpenses[0].Title);

        Assert.NotNull(result.ExecutedBy);
        Assert.Equal(ivy.Id, result.ExecutedBy!.UserId);
        Assert.Equal("艾薇", result.ExecutedBy.Name);
        Assert.NotNull(result.ExecutedAt);

        var ivyDetail = result.SplitDetails.Single(d => d.UserId == ivy.Id);
        var aweiDetail = result.SplitDetails.Single(d => d.UserId == awei.Id);
        var aheDetail = result.SplitDetails.Single(d => d.UserId == ahe.Id);

        Assert.True(ivyDetail.IsPayer);
        Assert.Equal(2000m, ivyDetail.ReceivableAmount);
        Assert.Equal(0m, ivyDetail.PayableAmount);

        Assert.False(aweiDetail.IsPayer);
        Assert.Equal(1000m, aweiDetail.PayableAmount);
        Assert.Equal(0m, aweiDetail.ReceivableAmount);

        Assert.False(aheDetail.IsPayer);
        Assert.Equal(1000m, aheDetail.PayableAmount);
        Assert.Equal(0m, aheDetail.ReceivableAmount);
    }

    [Fact]
    public async Task GetSplitPreviewAsync_WithUnknownSplitBatchId_ThrowsNotFound()
    {
        var careGroupId = Guid.NewGuid();
        var repository = new FakeExpenseRepository();
        var groupRepository = new FakeCareGroupRepository(isMember: true);
        var splitRepository = new FakeExpenseSplitRepository();
        var userRepository = new FakeUserRepository();
        var service = new ExpenseService(repository, groupRepository, userRepository, splitRepository);

        var exception = await Assert.ThrowsAsync<DomainException>(() =>
            service.GetSplitPreviewAsync(Guid.NewGuid(), careGroupId, new SplitPreviewQueryRequest
            {
                SplitBatchId = Guid.NewGuid()
            }));

        Assert.Equal(404, exception.StatusCode);
        Assert.Equal("NOT_FOUND", exception.ErrorCode);
    }

    [Fact]
    public async Task GetMemberExpenseTotalsAsync_ShareView_AggregatesFromExpenseSplits()
    {
        var careGroupId = Guid.NewGuid();
        var payerId = Guid.NewGuid();
        var memberA = Guid.NewGuid();
        var memberB = Guid.NewGuid();

        var users = new[]
        {
            new User { Id = payerId, Username = "Payer" },
            new User { Id = memberA, Username = "MemberA" },
            new User { Id = memberB, Username = "MemberB" }
        };

        var careGroupRepository = new FakeCareGroupRepository(isMember: true)
        {
            Members = new List<CareGroupMember>
            {
                new CareGroupMember { CareGroupId = careGroupId, UserId = payerId, User = users[0] },
                new CareGroupMember { CareGroupId = careGroupId, UserId = memberA, User = users[1] },
                new CareGroupMember { CareGroupId = careGroupId, UserId = memberB, User = users[2] }
            }
        };

        var splitRepository = new FakeExpenseSplitRepository();
        // 模擬一筆 1200 已分給 3 人各 400
        splitRepository.Splits.AddRange(new[]
        {
            new ExpenseSplit { CareGroupId = careGroupId, UserId = payerId, ShareAmount = 400m, IsPayer = true },
            new ExpenseSplit { CareGroupId = careGroupId, UserId = memberA, ShareAmount = 400m, IsPayer = false },
            new ExpenseSplit { CareGroupId = careGroupId, UserId = memberB, ShareAmount = 400m, IsPayer = false }
        });

        var repository = new FakeExpenseRepository();
        var userRepository = new FakeUserRepository(users);
        var service = new ExpenseService(repository, careGroupRepository, userRepository, splitRepository);

        var result = await service.GetMemberExpenseTotalsAsync(payerId, careGroupId, new MemberExpenseTotalsRequest
        {
            Scope = "all"
        });

        // share 視角從 ExpenseSplit 聚合，每人 400
        Assert.Equal(1200m, result.ShareTotalAmount);
        Assert.Equal(0m, result.PayerTotalAmount); // 沒有 Pending 的 ExpenseRecord
        Assert.Equal(3, result.MemberCount);
        Assert.All(result.Members, m => Assert.Equal(400m, m.ShareTotal));
        Assert.All(result.Members, m => Assert.Equal(1, m.ShareCount));
        Assert.All(result.Members, m => Assert.Equal(0m, m.PayerTotal));
        Assert.All(result.Members, m => Assert.Equal(0, m.PayerCount));
    }

    [Fact]
    public async Task GetMemberExpenseTotalsAsync_PersonalPayable_CombinesShareAndSelfExpense()
    {
        var careGroupId = Guid.NewGuid();
        var payerId = Guid.NewGuid();
        var memberA = Guid.NewGuid();
        var memberB = Guid.NewGuid();

        var users = new[]
        {
            new User { Id = payerId, Username = "Payer" },
            new User { Id = memberA, Username = "MemberA" },
            new User { Id = memberB, Username = "MemberB" }
        };

        var careGroupRepository = new FakeCareGroupRepository(isMember: true)
        {
            Members = new List<CareGroupMember>
            {
                new CareGroupMember { CareGroupId = careGroupId, UserId = payerId, User = users[0] },
                new CareGroupMember { CareGroupId = careGroupId, UserId = memberA, User = users[1] },
                new CareGroupMember { CareGroupId = careGroupId, UserId = memberB, User = users[2] }
            }
        };

        var repository = new FakeExpenseRepository(
            new ExpenseRecord
            {
                Id = Guid.NewGuid(),
                CareGroupId = careGroupId,
                Title = "Self Expense",
                Amount = 500m,
                SplitStatus = ExpenseSplitStatus.None,
                CreatedBy = payerId.ToString(),
                ExpenseDate = DateTime.UtcNow
            });

        var splitRepository = new FakeExpenseSplitRepository();
        splitRepository.Splits.AddRange(new[]
        {
            new ExpenseSplit { CareGroupId = careGroupId, UserId = payerId, ShareAmount = 1000m, IsPayer = true },
            new ExpenseSplit { CareGroupId = careGroupId, UserId = memberA, ShareAmount = 1000m, IsPayer = false },
            new ExpenseSplit { CareGroupId = careGroupId, UserId = memberB, ShareAmount = 1000m, IsPayer = false }
        });

        var userRepository = new FakeUserRepository(users);
        var service = new ExpenseService(repository, careGroupRepository, userRepository, splitRepository);

        var result = await service.GetMemberExpenseTotalsAsync(payerId, careGroupId, new MemberExpenseTotalsRequest
        {
            Scope = "all"
        });

        var payerItem = result.Members.First(x => x.UserId == payerId);
        Assert.Equal(1000m, payerItem.ShareTotal);
        Assert.Equal(500m, payerItem.SelfExpenseTotal);
        Assert.Equal(1500m, payerItem.PersonalPayableTotal);
        Assert.Equal(1500m, payerItem.CurrentPayableTotal);
        Assert.Equal(1, payerItem.SelfExpenseCount);

        Assert.Equal(500m, result.SelfExpenseTotalAmount);
        Assert.Equal(3500m, result.PersonalPayableTotalAmount);
        Assert.Equal(3500m, result.CurrentPayableTotalAmount);
    }

    [Fact]
    public async Task GetMemberExpenseTotalsAsync_CurrentPayable_IncludesPendingShareAndSelfExpense()
    {
        var careGroupId = Guid.NewGuid();
        var payerId = Guid.NewGuid();

        var users = new[]
        {
            new User { Id = payerId, Username = "Payer" }
        };

        var careGroupRepository = new FakeCareGroupRepository(isMember: true)
        {
            Members = new List<CareGroupMember>
            {
                new CareGroupMember { CareGroupId = careGroupId, UserId = payerId, User = users[0] }
            }
        };

        var repository = new FakeExpenseRepository(
            new ExpenseRecord
            {
                Id = Guid.NewGuid(),
                CareGroupId = careGroupId,
                Title = "Pending Expense",
                Amount = 200m,
                SplitStatus = ExpenseSplitStatus.Pending,
                CreatedBy = payerId.ToString(),
                ExpenseDate = DateTime.UtcNow
            },
            new ExpenseRecord
            {
                Id = Guid.NewGuid(),
                CareGroupId = careGroupId,
                Title = "Self Expense",
                Amount = 500m,
                SplitStatus = ExpenseSplitStatus.None,
                CreatedBy = payerId.ToString(),
                ExpenseDate = DateTime.UtcNow
            });

        var splitRepository = new FakeExpenseSplitRepository();
        splitRepository.Splits.Add(new ExpenseSplit
        {
            CareGroupId = careGroupId,
            UserId = payerId,
            ShareAmount = 1000m,
            IsPayer = true
        });

        var userRepository = new FakeUserRepository(users);
        var service = new ExpenseService(repository, careGroupRepository, userRepository, splitRepository);

        var result = await service.GetMemberExpenseTotalsAsync(payerId, careGroupId, new MemberExpenseTotalsRequest
        {
            Scope = "all"
        });

        var payerItem = result.Members.Single();
        Assert.Equal(200m, payerItem.PayerTotal);
        Assert.Equal(1000m, payerItem.ShareTotal);
        Assert.Equal(500m, payerItem.SelfExpenseTotal);
        Assert.Equal(1500m, payerItem.PersonalPayableTotal);
        Assert.Equal(1700m, payerItem.CurrentPayableTotal);
        Assert.Equal(1700m, result.CurrentPayableTotalAmount);
    }

    private sealed class FakeExpenseRepository : IExpenseRepository
    {
        public List<ExpenseRecord> Expenses { get; } = new();

        public FakeExpenseRepository(params ExpenseRecord[] expenses)
        {
            Expenses.AddRange(expenses);
        }

        public Task<ExpenseRecord?> GetByIdAsync(Guid id)
        {
            return Task.FromResult(Expenses.FirstOrDefault(x => x.Id == id && x.DeletedAt == null));
        }

        public Task<(List<ExpenseRecord> Data, int TotalCount)> GetPagedByCareGroupIdAsync(Guid careGroupId, int page, int pageSize, string sort)
        {
            var data = Expenses.Where(x => x.CareGroupId == careGroupId && x.DeletedAt == null).ToList();
            return Task.FromResult((data, data.Count));
        }

        public Task<(List<ExpenseRecord> Data, int TotalCount)> GetPagedByCareGroupIdAsync(Guid careGroupId, PaginationRequest request)
        {
            var data = Expenses.Where(x => x.CareGroupId == careGroupId && x.DeletedAt == null).ToList();
            return Task.FromResult((data, data.Count));
        }

        public Task<List<ExpenseRecord>> GetListByIdsAsync(Guid careGroupId, IEnumerable<Guid> expenseIds)
        {
            var data = Expenses.Where(x => x.CareGroupId == careGroupId && expenseIds.Contains(x.Id)).ToList();
            return Task.FromResult(data);
        }

        public Task<List<ExpenseRecord>> GetPendingByDateRangeAsync(Guid careGroupId, DateTime dateFrom, DateTime dateTo)
        {
            var data = Expenses
                .Where(x => x.CareGroupId == careGroupId
                    && x.ExpenseDate >= dateFrom && x.ExpenseDate < dateTo
                    && x.SplitStatus == ExpenseSplitStatus.Pending
                    && x.DeletedAt == null)
                .OrderBy(x => x.ExpenseDate)
                .ToList();
            return Task.FromResult(data);
        }

        public Task<List<ExpenseRecord>> GetByCareGroupAsync(Guid careGroupId, DateTime? dateFrom, DateTime? dateTo, ExpenseSplitStatus? status)
        {
            var query = Expenses.Where(x => x.CareGroupId == careGroupId && x.DeletedAt == null);
            if (dateFrom.HasValue) query = query.Where(x => x.ExpenseDate >= dateFrom.Value);
            if (dateTo.HasValue) query = query.Where(x => x.ExpenseDate < dateTo.Value);
            if (status.HasValue) query = query.Where(x => x.SplitStatus == status.Value);
            return Task.FromResult(query.OrderBy(x => x.ExpenseDate).ToList());
        }

        public Task AddAsync(ExpenseRecord expense)
        {
            Expenses.Add(expense);
            return Task.CompletedTask;
        }

        public Task UpdateAsync(ExpenseRecord expense)
        {
            return Task.CompletedTask;
        }

        public Task SaveChangesAsync()
        {
            return Task.CompletedTask;
        }
    }

    private sealed class FakeExpenseSplitRepository : IExpenseSplitRepository
    {
        public List<ExpenseSplit> Splits { get; } = new();

        public Task AddRangeAsync(IEnumerable<ExpenseSplit> splits)
        {
            Splits.AddRange(splits);
            return Task.CompletedTask;
        }

        public Task<List<ExpenseSplit>> GetByCareGroupAsync(Guid careGroupId, DateTime? dateFrom, DateTime? dateTo)
        {
            // Fake 不模擬 Include(Expense)，因此忽略日期過濾，由呼叫端準備好資料即可。
            return Task.FromResult(Splits.Where(x => x.CareGroupId == careGroupId && x.DeletedAt == null).ToList());
        }

        public Task<List<ExpenseSplit>> GetByBatchIdAsync(Guid careGroupId, Guid splitBatchId)
        {
            return Task.FromResult(Splits
                .Where(x => x.CareGroupId == careGroupId
                            && x.SplitBatchId == splitBatchId
                            && x.DeletedAt == null)
                .ToList());
        }

        public Task<Dictionary<Guid, Guid>> GetBatchIdsByExpenseIdsAsync(Guid careGroupId, IEnumerable<Guid> expenseIds)
        {
            var idSet = expenseIds.ToHashSet();
            var dict = Splits
                .Where(x => x.CareGroupId == careGroupId
                            && x.SplitBatchId.HasValue
                            && x.DeletedAt == null
                            && idSet.Contains(x.ExpenseId))
                .GroupBy(x => x.ExpenseId)
                .ToDictionary(g => g.Key, g => g.First().SplitBatchId!.Value);
            return Task.FromResult(dict);
        }

        public Task SaveChangesAsync() => Task.CompletedTask;
    }

    private sealed class FakeCareGroupRepository : ICareGroupRepository
    {
        private readonly bool _isMember;

        public FakeCareGroupRepository(bool isMember)
        {
            _isMember = isMember;
        }

        public Task<CareGroup?> GetByIdAsync(Guid id)
        {
            return Task.FromResult<CareGroup?>(null);
        }

        public Task<CareGroup?> GetByInviteCodeAsync(string inviteCode)
        {
            return Task.FromResult<CareGroup?>(null);
        }

        public Task<(List<CareGroup> Data, int TotalCount)> GetPagedByUserIdAsync(Guid userId, int page, int pageSize, string sort)
        {
            return Task.FromResult((new List<CareGroup>(), 0));
        }

        public Task<List<Guid>> GetAccessibleCareGroupIdsAsync(Guid userId)
        {
            return Task.FromResult(new List<Guid>());
        }

        public Task AddAsync(CareGroup careGroup)
        {
            return Task.CompletedTask;
        }

        public Task AddMemberAsync(CareGroupMember member)
        {
            return Task.CompletedTask;
        }

        public Task<bool> IsMemberAsync(Guid careGroupId, Guid userId)
        {
            return Task.FromResult(_isMember);
        }

        public Task<CareGroupMember?> GetMemberAsync(Guid careGroupId, Guid userId)
        {
            return Task.FromResult<CareGroupMember?>(null);
        }

        public Task<CareGroupMember?> GetMemberIncludingDeletedAsync(Guid careGroupId, Guid userId)
        {
            return Task.FromResult<CareGroupMember?>(null);
        }

        public Task<List<CareGroupMember>> GetActiveMembersWithUserAsync(Guid careGroupId)
        {
            return Task.FromResult(Members);
        }

        public List<CareGroupMember> Members { get; set; } = new();

        public Task SaveChangesAsync()
        {
            return Task.CompletedTask;
        }
    }

    private sealed class FakeUserRepository : IUserRepository
    {
        public List<User> Users { get; } = new();

        public FakeUserRepository(params User[] users)
        {
            Users.AddRange(users);
        }

        public Task<bool> EmailExistsAsync(string email) => Task.FromResult(false);

        public Task<User?> GetByEmailAsync(string email) => Task.FromResult<User?>(null);

        public Task<User?> GetByIdAsync(Guid id) => Task.FromResult(Users.FirstOrDefault(x => x.Id == id));

        public Task<List<User>> GetListByIdsAsync(IEnumerable<Guid> ids) => Task.FromResult(Users.Where(x => ids.Contains(x.Id)).ToList());

        public Task AddAsync(User user) => Task.CompletedTask;

        public Task UpdateAsync(User user) => Task.CompletedTask;

        public Task SaveChangesAsync() => Task.CompletedTask;
    }
}
