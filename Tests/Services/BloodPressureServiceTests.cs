using SeasonsCare.Api.DTOs.Common;
using SeasonsCare.Api.DTOs.HealthRecords.BloodPressures;
using SeasonsCare.Api.Exceptions;
using SeasonsCare.Api.Models.Entities;
using SeasonsCare.Api.Models.Entities.HealthRecords;
using SeasonsCare.Api.Repositories;
using SeasonsCare.Api.Repositories.HealthRecords;
using SeasonsCare.Api.Services.HealthRecords;

namespace SeasonsCare.Api.Tests.Services;

public class BloodPressureServiceTests
{
    [Fact]
    public async Task CreateRecordAsync_ThrowsForbidden_WhenUserIsNotMember()
    {
        var repository = new FakeBloodPressureRepository();
        var groupRepository = new FakeCareGroupRepository(isMember: false);
        var service = new BloodPressureService(repository, groupRepository);

        var exception = await Assert.ThrowsAsync<DomainException>(() =>
            service.CreateRecordAsync(Guid.NewGuid(), Guid.NewGuid(), new CreateBloodPressureRequest
            {
                Systolic = 120,
                Diastolic = 80
            }));

        Assert.Equal(403, exception.StatusCode);
        Assert.Equal("FORBIDDEN", exception.ErrorCode);
    }

    [Fact]
    public async Task UpdateRecordAsync_ThrowsConflict_WhenUpdatedAtDoesNotMatch()
    {
        var existing = new BloodPressureRecord
        {
            Id = Guid.NewGuid(),
            CareGroupId = Guid.NewGuid(),
            Systolic = 120,
            Diastolic = 80,
            UpdatedAt = new DateTime(2026, 3, 20, 2, 0, 0, DateTimeKind.Utc)
        };

        var repository = new FakeBloodPressureRepository(existing);
        var groupRepository = new FakeCareGroupRepository(isMember: true);
        var service = new BloodPressureService(repository, groupRepository);

        var exception = await Assert.ThrowsAsync<DomainException>(() =>
            service.UpdateRecordAsync(Guid.NewGuid(), existing.CareGroupId, existing.Id, new UpdateBloodPressureRequest
            {
                Systolic = 130,
                Diastolic = 85,
                UpdatedAt = existing.UpdatedAt.AddMinutes(-1)
            }));

        Assert.Equal(409, exception.StatusCode);
        Assert.Equal("CONCURRENCY_CONFLICT", exception.ErrorCode);
    }

    [Fact]
    public async Task CreateRecordAsync_CreatesRecord_WhenInputIsValid()
    {
        var repository = new FakeBloodPressureRepository();
        var groupRepository = new FakeCareGroupRepository(isMember: true);
        var service = new BloodPressureService(repository, groupRepository);
        var userId = Guid.NewGuid();
        var careGroupId = Guid.NewGuid();

        var result = await service.CreateRecordAsync(userId, careGroupId, new CreateBloodPressureRequest
        {
            Systolic = 118,
            Diastolic = 76
        });

        Assert.Equal(careGroupId, result.CareGroupId);
        Assert.Equal(userId.ToString(), result.CreatedBy);
        Assert.Single(repository.Items);
    }

    private sealed class FakeBloodPressureRepository : IBloodPressureRepository
    {
        public List<BloodPressureRecord> Items { get; } = new();

        public FakeBloodPressureRepository(params BloodPressureRecord[] items)
        {
            Items.AddRange(items);
        }

        public Task<PagedResponse<BloodPressureRecord>> GetPagedAsync(Guid careGroupId, PaginationRequest request)
        {
            var data = Items.Where(x => x.CareGroupId == careGroupId && x.DeletedAt == null).ToList();
            return Task.FromResult(new PagedResponse<BloodPressureRecord>(data, data.Count, request.Page, request.PageSize));
        }

        public Task<BloodPressureRecord?> GetByIdAsync(Guid careGroupId, Guid id)
        {
            return Task.FromResult(Items.FirstOrDefault(x => x.CareGroupId == careGroupId && x.Id == id && x.DeletedAt == null));
        }

        public Task<BloodPressureRecord> AddAsync(BloodPressureRecord record)
        {
            Items.Add(record);
            return Task.FromResult(record);
        }

        public Task<BloodPressureRecord> UpdateAsync(BloodPressureRecord record)
        {
            return Task.FromResult(record);
        }
    }

    private sealed class FakeCareGroupRepository : ICareGroupRepository
    {
        private readonly bool _isMember;

        public FakeCareGroupRepository(bool isMember)
        {
            _isMember = isMember;
        }

        public Task<CareGroup?> GetByIdAsync(Guid id) => Task.FromResult<CareGroup?>(null);
        public Task<(List<CareGroup> Data, int TotalCount)> GetPagedByUserIdAsync(Guid userId, int page, int pageSize, string sort) => Task.FromResult((new List<CareGroup>(), 0));
        public Task<List<Guid>> GetAccessibleCareGroupIdsAsync(Guid userId) => Task.FromResult(new List<Guid>());
        public Task AddAsync(CareGroup careGroup) => Task.CompletedTask;
        public Task AddMemberAsync(CareGroupMember member) => Task.CompletedTask;
        public Task<bool> IsMemberAsync(Guid careGroupId, Guid userId) => Task.FromResult(_isMember);
        public Task<CareGroupMember?> GetMemberAsync(Guid careGroupId, Guid userId) => Task.FromResult<CareGroupMember?>(null);
        public Task<CareGroupMember?> GetMemberIncludingDeletedAsync(Guid careGroupId, Guid userId) => Task.FromResult<CareGroupMember?>(null);
        public Task SaveChangesAsync() => Task.CompletedTask;
    }
}
