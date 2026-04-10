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
        var service = new ExpenseService(repository, groupRepository);

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
        var service = new ExpenseService(repository, groupRepository);
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
        var service = new ExpenseService(repository, groupRepository);

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
        var service = new ExpenseService(repository, groupRepository);

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
        var service = new ExpenseService(repository, groupRepository);

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
}
