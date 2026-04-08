using SeasonsCare.Api.DTOs.EventSeries;
using SeasonsCare.Api.Exceptions;
using SeasonsCare.Api.Models.Entities;
using SeasonsCare.Api.Models.Enums;
using SeasonsCare.Api.Repositories;
using SeasonsCare.Api.Services;

namespace SeasonsCare.Api.Tests.Services;

public class EventSeriesServiceTests
{
    [Fact]
    public async Task CreateSeriesAsync_CreatesSeries_WhenParticipantsBelongToGroup()
    {
        var repository = new FakeEventSeriesRepository();
        var creatorId = Guid.NewGuid();
        var participantId = Guid.NewGuid();
        var careGroupId = Guid.NewGuid();
        var groupRepository = new FakeCareGroupRepository(isMember: true, groupMembers: new[] { creatorId, participantId });
        var service = new EventSeriesService(repository, groupRepository);

        var result = await service.CreateSeriesAsync(creatorId, careGroupId, new CreateEventSeriesRequest
        {
            Title = "每週回診",
            StartsAt = new DateTime(2026, 4, 13, 9, 0, 0, DateTimeKind.Utc),
            RepeatPattern = EventRepeatPattern.Weekly,
            RepeatInterval = 1,
            DaysOfWeek = new List<DayOfWeek> { DayOfWeek.Monday },
            EndType = EventSeriesEndType.OnDate,
            EndAt = new DateTime(2026, 5, 4, 9, 0, 0, DateTimeKind.Utc),
            Participants = new List<string> { creatorId.ToString(), participantId.ToString() },
            Status = "scheduled",
            IsImportant = true
        });

        Assert.NotEqual(Guid.Empty, result.Id);
        Assert.Equal("每週回診", result.Title);
        Assert.Equal(EventRepeatPattern.Weekly, result.RepeatPattern);
        Assert.Equal(new[] { DayOfWeek.Monday }, result.DaysOfWeek);
        Assert.Equal(new[] { creatorId.ToString(), participantId.ToString() }, result.Participants);
        Assert.Single(repository.Items);
    }

    [Fact]
    public async Task CreateSeriesAsync_ThrowsBadRequest_WhenParticipantIsNotGroupMember()
    {
        var repository = new FakeEventSeriesRepository();
        var creatorId = Guid.NewGuid();
        var careGroupId = Guid.NewGuid();
        var groupRepository = new FakeCareGroupRepository(isMember: true, groupMembers: new[] { creatorId });
        var service = new EventSeriesService(repository, groupRepository);

        var exception = await Assert.ThrowsAsync<DomainException>(() =>
            service.CreateSeriesAsync(creatorId, careGroupId, new CreateEventSeriesRequest
            {
                Title = "每週回診",
                StartsAt = DateTime.UtcNow,
                Participants = new List<string> { Guid.NewGuid().ToString() }
            }));

        Assert.Equal(400, exception.StatusCode);
        Assert.Equal("VALIDATION_FAILED", exception.ErrorCode);
    }

    private sealed class FakeEventSeriesRepository : IEventSeriesRepository
    {
        public List<EventSeries> Items { get; } = new();

        public Task<EventSeries?> GetByIdAsync(Guid id)
        {
            return Task.FromResult(Items.FirstOrDefault(x => x.Id == id && x.DeletedAt == null));
        }

        public Task<(List<EventSeries> Data, int TotalCount)> GetPagedByCareGroupIdAsync(Guid careGroupId, int page, int pageSize, string sort)
        {
            var data = Items.Where(x => x.CareGroupId == careGroupId && x.DeletedAt == null).ToList();
            return Task.FromResult((data, data.Count));
        }

        public Task AddAsync(EventSeries series)
        {
            Items.Add(series);
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

        public Task AddAsync(CareGroup careGroup) => Task.CompletedTask;
        public Task AddMemberAsync(CareGroupMember member) => Task.CompletedTask;
        public Task<bool> IsMemberAsync(Guid careGroupId, Guid userId) => Task.FromResult(_isMember);
        public Task<CareGroupMember?> GetMemberAsync(Guid careGroupId, Guid userId) => Task.FromResult<CareGroupMember?>(null);
        public Task<CareGroupMember?> GetMemberIncludingDeletedAsync(Guid careGroupId, Guid userId) => Task.FromResult<CareGroupMember?>(null);
        public Task SaveChangesAsync() => Task.CompletedTask;
    }
}
