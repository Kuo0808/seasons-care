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
        var service = new ExpenseService(repository, groupRepository, userRepository);

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
        var service = new ExpenseService(repository, groupRepository, userRepository);
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
        var service = new ExpenseService(repository, groupRepository, userRepository);

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
        var service = new ExpenseService(repository, groupRepository, userRepository);

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
        var service = new ExpenseService(repository, groupRepository, userRepository);

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
        var service = new ExpenseService(repository, groupRepository, userRepository);

        var result = await service.PreviewSplitAsync(payerId, careGroupId, new SplitPreviewRequest
        {
            SplitMode = "custom",
            ExpenseIds = new List<Guid> { existingExpenses[0].Id, existingExpenses[1].Id },
            TargetUserIds = new List<Guid> { payerId, anotherPayerId, nonPayerId } // 3 users
        });

        Assert.Equal(1200m, result.TotalAmount); // 1000 + 200
        Assert.Equal(2, result.SelectedExpenses.Count);
        Assert.Equal(3, result.SplitDetails.Count);

        var share = 1200m / 3m; // 400

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
