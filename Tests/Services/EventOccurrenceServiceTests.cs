using SeasonsCare.Api.Models.Entities;
using SeasonsCare.Api.Models.Enums;
using SeasonsCare.Api.Repositories;
using SeasonsCare.Api.Services;

namespace SeasonsCare.Api.Tests.Services;

public class EventOccurrenceServiceTests
{
    [Fact]
    public async Task GetOccurrencesAsync_ExpandsWeeklySeriesWithinRange()
    {
        var careGroupId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var participantId = Guid.NewGuid();
        var groupRepository = new FakeCareGroupRepository(isMember: true);
        var seriesRepository = new FakeEventSeriesRepository(
            new EventSeries
            {
                Id = Guid.NewGuid(),
                CareGroupId = careGroupId,
                Title = "Weekly Clinic",
                Description = "Recurring appointment",
                StartsAt = new DateTime(2026, 4, 6, 9, 0, 0, DateTimeKind.Utc),
                DurationMinutes = 60,
                RepeatPattern = EventRepeatPattern.Weekly,
                RepeatInterval = 1,
                DaysOfWeekMask = 1 << (int)DayOfWeek.Monday,
                EndType = EventSeriesEndType.Never,
                Participants = new[] { userId.ToString(), participantId.ToString() },
                Status = "scheduled",
                IsImportant = true,
                CreatedBy = userId.ToString()
            });
        var occurrenceRepository = new FakeEventOccurrenceRepository();
        var service = new EventOccurrenceService(seriesRepository, occurrenceRepository, groupRepository);

        var items = await service.GetOccurrencesAsync(
            userId,
            careGroupId,
            new DateTime(2026, 4, 1, 0, 0, 0, DateTimeKind.Utc),
            new DateTime(2026, 4, 30, 23, 59, 59, DateTimeKind.Utc));

        Assert.Equal(4, items.Count);
        Assert.All(items, item => Assert.Equal(DayOfWeek.Monday, item.ScheduledStartAt.DayOfWeek));
        Assert.All(items, item => Assert.Equal(new[] { userId.ToString(), participantId.ToString() }, item.Participants));
        Assert.All(items, item => Assert.Equal(EventOccurrenceStatus.Scheduled, item.Status));
    }

    [Fact]
    public async Task CancelOccurrenceAsync_CreatesCancelledOverride_ForSingleOccurrence()
    {
        var careGroupId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var seriesId = Guid.NewGuid();
        var seriesRepository = new FakeEventSeriesRepository(
            new EventSeries
            {
                Id = seriesId,
                CareGroupId = careGroupId,
                Title = "Weekly Clinic",
                StartsAt = new DateTime(2026, 4, 6, 9, 0, 0, DateTimeKind.Utc),
                RepeatPattern = EventRepeatPattern.Weekly,
                RepeatInterval = 1,
                DaysOfWeekMask = 1 << (int)DayOfWeek.Monday,
                EndType = EventSeriesEndType.Never,
                Participants = new[] { userId.ToString() },
                CreatedBy = userId.ToString()
            });
        var occurrenceRepository = new FakeEventOccurrenceRepository();
        var groupRepository = new FakeCareGroupRepository(isMember: true);
        var service = new EventOccurrenceService(seriesRepository, occurrenceRepository, groupRepository);
        var thirdWeek = new DateTime(2026, 4, 20, 9, 0, 0, DateTimeKind.Utc);

        await service.CancelOccurrenceAsync(userId, careGroupId, seriesId, thirdWeek);

        var items = await service.GetOccurrencesAsync(
            userId,
            careGroupId,
            new DateTime(2026, 4, 1, 0, 0, 0, DateTimeKind.Utc),
            new DateTime(2026, 4, 30, 23, 59, 59, DateTimeKind.Utc));

        var cancelled = items.Single(x => x.ScheduledStartAt == thirdWeek);
        Assert.Equal(EventOccurrenceStatus.Cancelled, cancelled.Status);
        Assert.True(cancelled.HasOverrides);
        Assert.Single(occurrenceRepository.Items);
    }

    [Fact]
    public async Task CompleteOccurrenceAsync_CreatesCompletedOverride_ForSingleOccurrence()
    {
        var careGroupId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var seriesId = Guid.NewGuid();
        var seriesRepository = new FakeEventSeriesRepository(
            new EventSeries
            {
                Id = seriesId,
                CareGroupId = careGroupId,
                Title = "Medication Reminder",
                StartsAt = new DateTime(2026, 4, 6, 15, 0, 0, DateTimeKind.Utc),
                RepeatPattern = EventRepeatPattern.Daily,
                RepeatInterval = 1,
                EndType = EventSeriesEndType.Never,
                Participants = new[] { userId.ToString() },
                CreatedBy = userId.ToString()
            });
        var occurrenceRepository = new FakeEventOccurrenceRepository();
        var groupRepository = new FakeCareGroupRepository(isMember: true);
        var service = new EventOccurrenceService(seriesRepository, occurrenceRepository, groupRepository);
        var targetOccurrence = new DateTime(2026, 4, 8, 15, 0, 0, DateTimeKind.Utc);

        await service.CompleteOccurrenceAsync(userId, careGroupId, seriesId, targetOccurrence);

        var items = await service.GetOccurrencesAsync(
            userId,
            careGroupId,
            new DateTime(2026, 4, 1, 0, 0, 0, DateTimeKind.Utc),
            new DateTime(2026, 4, 30, 23, 59, 59, DateTimeKind.Utc));

        var completed = items.Single(x => x.ScheduledStartAt == targetOccurrence);
        Assert.Equal(EventOccurrenceStatus.Completed, completed.Status);
        Assert.True(completed.HasOverrides);
        Assert.Single(occurrenceRepository.Items);
    }

    [Fact]
    public async Task GetOccurrencesAsync_ExpandsDailySeriesWithinRange()
    {
        var careGroupId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var groupRepository = new FakeCareGroupRepository(isMember: true);
        var seriesRepository = new FakeEventSeriesRepository(
            new EventSeries
            {
                Id = Guid.NewGuid(),
                CareGroupId = careGroupId,
                Title = "Daily Check",
                StartsAt = new DateTime(2026, 4, 1, 8, 30, 0, DateTimeKind.Utc),
                RepeatPattern = EventRepeatPattern.Daily,
                RepeatInterval = 2,
                EndType = EventSeriesEndType.Never,
                Participants = new[] { userId.ToString() },
                CreatedBy = userId.ToString()
            });
        var occurrenceRepository = new FakeEventOccurrenceRepository();
        var service = new EventOccurrenceService(seriesRepository, occurrenceRepository, groupRepository);

        var items = await service.GetOccurrencesAsync(
            userId,
            careGroupId,
            new DateTime(2026, 4, 1, 0, 0, 0, DateTimeKind.Utc),
            new DateTime(2026, 4, 7, 23, 59, 59, DateTimeKind.Utc));

        Assert.Equal(4, items.Count);
        Assert.Equal(new DateTime(2026, 4, 1, 8, 30, 0, DateTimeKind.Utc), items[0].ScheduledStartAt);
        Assert.Equal(new DateTime(2026, 4, 3, 8, 30, 0, DateTimeKind.Utc), items[1].ScheduledStartAt);
        Assert.Equal(new DateTime(2026, 4, 5, 8, 30, 0, DateTimeKind.Utc), items[2].ScheduledStartAt);
        Assert.Equal(new DateTime(2026, 4, 7, 8, 30, 0, DateTimeKind.Utc), items[3].ScheduledStartAt);
    }

    [Fact]
    public async Task GetOccurrencesAsync_ExpandsMonthlySeriesAndClampsMissingDayToMonthEnd()
    {
        var careGroupId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var groupRepository = new FakeCareGroupRepository(isMember: true);
        var seriesRepository = new FakeEventSeriesRepository(
            new EventSeries
            {
                Id = Guid.NewGuid(),
                CareGroupId = careGroupId,
                Title = "Monthly Billing",
                StartsAt = new DateTime(2026, 1, 31, 10, 0, 0, DateTimeKind.Utc),
                RepeatPattern = EventRepeatPattern.Monthly,
                RepeatInterval = 1,
                EndType = EventSeriesEndType.Never,
                Participants = new[] { userId.ToString() },
                CreatedBy = userId.ToString()
            });
        var occurrenceRepository = new FakeEventOccurrenceRepository();
        var service = new EventOccurrenceService(seriesRepository, occurrenceRepository, groupRepository);

        var items = await service.GetOccurrencesAsync(
            userId,
            careGroupId,
            new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            new DateTime(2026, 4, 30, 23, 59, 59, DateTimeKind.Utc));

        Assert.Equal(4, items.Count);
        Assert.Equal(new DateTime(2026, 1, 31, 10, 0, 0, DateTimeKind.Utc), items[0].ScheduledStartAt);
        Assert.Equal(new DateTime(2026, 2, 28, 10, 0, 0, DateTimeKind.Utc), items[1].ScheduledStartAt);
        Assert.Equal(new DateTime(2026, 3, 31, 10, 0, 0, DateTimeKind.Utc), items[2].ScheduledStartAt);
        Assert.Equal(new DateTime(2026, 4, 30, 10, 0, 0, DateTimeKind.Utc), items[3].ScheduledStartAt);
    }

    private sealed class FakeEventSeriesRepository : IEventSeriesRepository
    {
        private readonly List<EventSeries> _items;

        public FakeEventSeriesRepository(params EventSeries[] items)
        {
            _items = items.ToList();
        }

        public Task<EventSeries?> GetByIdAsync(Guid id) => Task.FromResult(_items.FirstOrDefault(x => x.Id == id));

        public Task<List<EventSeries>> GetAllByCareGroupIdAsync(Guid careGroupId)
            => Task.FromResult(_items.Where(x => x.CareGroupId == careGroupId).ToList());

        public Task<(List<EventSeries> Data, int TotalCount)> GetPagedByCareGroupIdAsync(Guid careGroupId, int page, int pageSize, string sort)
        {
            var data = _items.Where(x => x.CareGroupId == careGroupId).ToList();
            return Task.FromResult((data, data.Count));
        }

        public Task AddAsync(EventSeries series)
        {
            _items.Add(series);
            return Task.CompletedTask;
        }

        public Task SaveChangesAsync() => Task.CompletedTask;
    }

    private sealed class FakeEventOccurrenceRepository : IEventOccurrenceRepository
    {
        public List<EventOccurrence> Items { get; } = new();

        public Task<List<EventOccurrence>> GetByRangeAsync(Guid careGroupId, DateTime from, DateTime to)
        {
            return Task.FromResult(Items.Where(x => x.CareGroupId == careGroupId && x.ScheduledStartAt >= from && x.ScheduledStartAt <= to).ToList());
        }

        public Task<EventOccurrence?> GetBySeriesIdAndScheduledStartAtAsync(Guid eventSeriesId, DateTime scheduledStartAt)
        {
            return Task.FromResult(Items.FirstOrDefault(x => x.EventSeriesId == eventSeriesId && x.ScheduledStartAt == scheduledStartAt));
        }

        public Task AddAsync(EventOccurrence occurrence)
        {
            Items.Add(occurrence);
            return Task.CompletedTask;
        }

        public Task UpdateAsync(EventOccurrence occurrence)
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

        public Task<CareGroup?> GetByIdAsync(Guid id) => Task.FromResult<CareGroup?>(null);
        public Task<CareGroup?> GetByInviteCodeAsync(string inviteCode) => Task.FromResult<CareGroup?>(null);
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
