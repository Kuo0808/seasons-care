using SeasonsCare.Api.DTOs.CareLogs;
using SeasonsCare.Api.DTOs.Common;
using SeasonsCare.Api.Exceptions;
using SeasonsCare.Api.Models.Entities;
using SeasonsCare.Api.Repositories;
using SeasonsCare.Api.Services;

namespace SeasonsCare.Api.Tests.Services;

public class CareLogServiceTests
{
    [Fact]
    public async Task CreateLogAsync_ThrowsForbidden_WhenUserIsNotMember()
    {
        var repository = new FakeCareLogRepository();
        var groupRepository = new FakeCareGroupRepository(isMember: false);
        var service = new CareLogService(repository, groupRepository);

        var exception = await Assert.ThrowsAsync<DomainException>(() =>
            service.CreateLogAsync(Guid.NewGuid(), Guid.NewGuid(), new CreateCareLogRequest
            {
                Title = "Morning update"
            }));

        Assert.Equal(403, exception.StatusCode);
        Assert.Equal("FORBIDDEN", exception.ErrorCode);
    }

    [Fact]
    public async Task CreateLogAsync_CreatesCareLog_WithFrontendFields()
    {
        var repository = new FakeCareLogRepository();
        var userId = Guid.NewGuid();
        var careGroupId = Guid.NewGuid();
        var memberUserId = Guid.NewGuid();
        var groupRepository = new FakeCareGroupRepository(isMember: true, groupMembers: new[] { userId, memberUserId });
        var service = new CareLogService(repository, groupRepository);

        var result = await service.CreateLogAsync(userId, careGroupId, new CreateCareLogRequest
        {
            Title = "Medication taken",
            Description = "After breakfast",
            StartsAt = new DateTime(2026, 3, 20, 1, 0, 0, DateTimeKind.Utc),
            RepeatPattern = "daily",
            Participants = new List<string> { userId.ToString(), memberUserId.ToString() },
            Status = "scheduled",
            IsImportant = true
        });

        Assert.NotEqual(Guid.Empty, result.Id);
        Assert.Equal(careGroupId, result.CareGroupId);
        Assert.Equal(userId.ToString(), result.CreatedBy);
        Assert.Equal("After breakfast", result.Description);
        Assert.Equal("daily", result.RepeatPattern);
        Assert.Equal(new[] { userId.ToString(), memberUserId.ToString() }, result.Participants);
        Assert.Equal("scheduled", result.Status);
        Assert.True(result.IsImportant);
        Assert.NotNull(result.UpdatedAt);
        Assert.Equal(result.CreatedAt, result.UpdatedAt);
        Assert.Single(repository.Logs);
    }

    [Fact]
    public async Task CreateLogAsync_ThrowsBadRequest_WhenParticipantIsNotGroupMember()
    {
        var repository = new FakeCareLogRepository();
        var userId = Guid.NewGuid();
        var careGroupId = Guid.NewGuid();
        var groupRepository = new FakeCareGroupRepository(isMember: true, groupMembers: new[] { userId });
        var service = new CareLogService(repository, groupRepository);

        var exception = await Assert.ThrowsAsync<DomainException>(() =>
            service.CreateLogAsync(userId, careGroupId, new CreateCareLogRequest
            {
                Title = "Medication taken",
                Participants = new List<string> { Guid.NewGuid().ToString() }
            }));

        Assert.Equal(400, exception.StatusCode);
        Assert.Equal("VALIDATION_FAILED", exception.ErrorCode);
    }

    [Fact]
    public async Task UpdateLogAsync_ThrowsConflict_WhenUpdatedAtIsMissing()
    {
        var existing = new CareLog
        {
            Id = Guid.NewGuid(),
            CareGroupId = Guid.NewGuid(),
            Title = "Initial title",
            UpdatedAt = DateTime.UtcNow
        };

        var repository = new FakeCareLogRepository(existing);
        var groupRepository = new FakeCareGroupRepository(isMember: true, groupMembers: new[] { Guid.NewGuid() });
        var service = new CareLogService(repository, groupRepository);

        var exception = await Assert.ThrowsAsync<DomainException>(() =>
            service.UpdateLogAsync(Guid.NewGuid(), existing.CareGroupId, existing.Id, new UpdateCareLogRequest
            {
                Title = "Changed title"
            }));

        Assert.Equal(409, exception.StatusCode);
        Assert.Equal("CONCURRENCY_CONFLICT", exception.ErrorCode);
    }

    [Fact]
    public async Task UpdateLogAsync_ThrowsConflict_WhenUpdatedAtDoesNotMatch()
    {
        var existing = new CareLog
        {
            Id = Guid.NewGuid(),
            CareGroupId = Guid.NewGuid(),
            Title = "Initial title",
            UpdatedAt = new DateTime(2026, 3, 20, 2, 0, 0, DateTimeKind.Utc)
        };

        var repository = new FakeCareLogRepository(existing);
        var groupRepository = new FakeCareGroupRepository(isMember: true, groupMembers: new[] { Guid.NewGuid() });
        var service = new CareLogService(repository, groupRepository);

        var exception = await Assert.ThrowsAsync<DomainException>(() =>
            service.UpdateLogAsync(Guid.NewGuid(), existing.CareGroupId, existing.Id, new UpdateCareLogRequest
            {
                Title = "Changed title",
                UpdatedAt = existing.UpdatedAt.Value.AddSeconds(-1)
            }));

        Assert.Equal(409, exception.StatusCode);
        Assert.Equal("CONCURRENCY_CONFLICT", exception.ErrorCode);
    }

    [Fact]
    public async Task UpdateLogAsync_UpdatesCareLog_WhenUpdatedAtMatches()
    {
        var existingUpdatedAt = new DateTime(2026, 3, 20, 2, 0, 0, 123, DateTimeKind.Utc);
        var memberUserId = Guid.NewGuid();
        var existing = new CareLog
        {
            Id = Guid.NewGuid(),
            CareGroupId = Guid.NewGuid(),
            Title = "Initial title",
            Description = "Old description",
            Status = "scheduled",
            StartsAt = new DateTime(2026, 3, 20, 1, 0, 0, DateTimeKind.Utc),
            Participants = new[] { memberUserId.ToString() },
            UpdatedAt = existingUpdatedAt
        };

        var repository = new FakeCareLogRepository(existing);
        var groupRepository = new FakeCareGroupRepository(isMember: true, groupMembers: new[] { memberUserId });
        var service = new CareLogService(repository, groupRepository);

        var result = await service.UpdateLogAsync(Guid.NewGuid(), existing.CareGroupId, existing.Id, new UpdateCareLogRequest
        {
            Title = "Changed title",
            Description = "New description",
            Status = "done",
            StartsAt = existing.StartsAt.AddHours(1),
            RepeatPattern = "weekly",
            Participants = new List<string> { memberUserId.ToString() },
            IsImportant = true,
            UpdatedAt = existingUpdatedAt
        });

        Assert.Equal("Changed title", result.Title);
        Assert.Equal("New description", result.Description);
        Assert.Equal("done", result.Status);
        Assert.Equal("weekly", result.RepeatPattern);
        Assert.Equal(new[] { memberUserId.ToString() }, result.Participants);
        Assert.True(result.IsImportant);
        Assert.NotNull(result.UpdatedAt);
        Assert.NotEqual(existingUpdatedAt, result.UpdatedAt.Value);
    }

    private sealed class FakeCareLogRepository : ICareLogRepository
    {
        public List<CareLog> Logs { get; } = new();

        public FakeCareLogRepository(params CareLog[] logs)
        {
            Logs.AddRange(logs);
        }

        public Task<CareLog?> GetByIdAsync(Guid id)
        {
            return Task.FromResult(Logs.FirstOrDefault(x => x.Id == id && x.DeletedAt == null));
        }

        public Task<(List<CareLog> Data, int TotalCount)> GetPagedByCareGroupIdAsync(Guid careGroupId, int page, int pageSize, string sort)
        {
            var data = Logs.Where(x => x.CareGroupId == careGroupId && x.DeletedAt == null).ToList();
            return Task.FromResult((data, data.Count));
        }

        public Task<(List<CareLog> Data, int TotalCount)> GetPagedByCareGroupIdAsync(Guid careGroupId, PaginationRequest request)
        {
            var data = Logs.Where(x => x.CareGroupId == careGroupId && x.DeletedAt == null).ToList();
            return Task.FromResult((data, data.Count));
        }

        public Task AddAsync(CareLog careLog)
        {
            Logs.Add(careLog);
            return Task.CompletedTask;
        }

        public Task UpdateAsync(CareLog careLog)
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
        private readonly Guid[] _groupMembers;

        public FakeCareGroupRepository(bool isMember, Guid[]? groupMembers = null)
        {
            _isMember = isMember;
            _groupMembers = groupMembers ?? Array.Empty<Guid>();
        }

        public Task<CareGroup?> GetByIdAsync(Guid id)
        {
            return Task.FromResult<CareGroup?>(new CareGroup
            {
                Id = id,
                Name = "Test Group",
                RecipientName = "Recipient",
                InviteCode = "TEST1234",
                Members = _groupMembers.Select(userId => new CareGroupMember
                {
                    Id = Guid.NewGuid(),
                    CareGroupId = id,
                    UserId = userId
                }).ToList()
            });
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
