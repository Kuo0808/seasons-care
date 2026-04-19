using SeasonsCare.Api.DTOs.EventOccurrences;
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
        var service = BuildWeeklyService(careGroupId, userId, seriesId, out var occurrenceRepository);
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
        Assert.Equal(thirdWeek, occurrenceRepository.Items[0].OccurrenceKeyStartAt);
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
    public async Task UpdateOccurrenceAsync_CreatesOverride_AndMovesOccurrence()
    {
        var careGroupId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var seriesId = Guid.NewGuid();
        var service = BuildWeeklyService(careGroupId, userId, seriesId, out var occurrenceRepository);
        var originalStart = new DateTime(2026, 4, 20, 9, 0, 0, DateTimeKind.Utc);
        var movedStart = new DateTime(2026, 4, 20, 11, 0, 0, DateTimeKind.Utc);
        var movedEnd = new DateTime(2026, 4, 20, 12, 30, 0, DateTimeKind.Utc);

        var updated = await service.UpdateOccurrenceAsync(userId, careGroupId, new UpdateEventOccurrenceRequest
        {
            EventSeriesId = seriesId,
            ScheduledStartAt = originalStart,
            Title = "Moved Clinic",
            Description = "Bring reports",
            StartsAt = movedStart,
            EndsAt = movedEnd,
            Participants = new List<string> { userId.ToString(), "helper" },
            IsImportant = false,
            Status = EventOccurrenceStatus.Scheduled
        });

        Assert.Equal(movedStart, updated.ScheduledStartAt.UtcDateTime);
        Assert.Equal(movedEnd, updated.ScheduledEndAt!.Value.UtcDateTime);
        Assert.Equal("Moved Clinic", updated.Title);
        Assert.Equal("Bring reports", updated.Description);
        Assert.Equal(new[] { userId.ToString(), "helper" }, updated.Participants);
        Assert.False(updated.IsImportant);
        Assert.True(updated.HasOverrides);

        var stored = Assert.Single(occurrenceRepository.Items);
        Assert.Equal(originalStart, stored.OccurrenceKeyStartAt);
        Assert.Equal(movedStart, stored.OverrideStartAt);
        Assert.Equal(movedEnd, stored.OverrideEndAt);
        Assert.False(stored.OverrideIsImportant);
    }

    [Fact]
    public async Task UpdateOccurrenceAsync_UsesDisplayedStartTime_ForExistingMovedOccurrence()
    {
        var careGroupId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var seriesId = Guid.NewGuid();
        var service = BuildWeeklyService(careGroupId, userId, seriesId, out _);
        var originalStart = new DateTime(2026, 4, 20, 9, 0, 0, DateTimeKind.Utc);
        var movedStart = new DateTime(2026, 4, 20, 11, 0, 0, DateTimeKind.Utc);

        await service.UpdateOccurrenceAsync(userId, careGroupId, new UpdateEventOccurrenceRequest
        {
            EventSeriesId = seriesId,
            ScheduledStartAt = originalStart,
            StartsAt = movedStart
        });

        var updated = await service.UpdateOccurrenceAsync(userId, careGroupId, new UpdateEventOccurrenceRequest
        {
            EventSeriesId = seriesId,
            ScheduledStartAt = movedStart,
            Title = "Follow-up Visit"
        });

        Assert.Equal(movedStart, updated.ScheduledStartAt.UtcDateTime);
        Assert.Equal("Follow-up Visit", updated.Title);
    }

    [Fact]
    public async Task ClearOccurrenceOverrideAsync_RemovesOverride_AndRestoresSeriesDefaults()
    {
        var careGroupId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var seriesId = Guid.NewGuid();
        var service = BuildWeeklyService(careGroupId, userId, seriesId, out var occurrenceRepository);
        var originalStart = new DateTime(2026, 4, 20, 9, 0, 0, DateTimeKind.Utc);
        var movedStart = new DateTime(2026, 4, 20, 11, 0, 0, DateTimeKind.Utc);

        await service.UpdateOccurrenceAsync(userId, careGroupId, new UpdateEventOccurrenceRequest
        {
            EventSeriesId = seriesId,
            ScheduledStartAt = originalStart,
            Title = "Moved Clinic",
            StartsAt = movedStart
        });

        await service.ClearOccurrenceOverrideAsync(userId, careGroupId, seriesId, movedStart);

        var items = await service.GetOccurrencesAsync(
            userId,
            careGroupId,
            new DateTime(2026, 4, 1, 0, 0, 0, DateTimeKind.Utc),
            new DateTime(2026, 4, 30, 23, 59, 59, DateTimeKind.Utc));

        var restored = items.Single(x => x.ScheduledStartAt == originalStart);
        Assert.Equal("Weekly Clinic", restored.Title);
        Assert.False(restored.HasOverrides);
        Assert.True(occurrenceRepository.Items[0].DeletedAt.HasValue);
    }

    [Fact]
    public async Task GetOccurrencesAsync_WeeklyOnMonday_UsesTaiwanDayOfWeek_WhenStartIsTaiwanMidnight()
    {
        // 前端送 startsAt = 2026-04-20 00:00 (+08:00, 週一)，落地後為 2026-04-19 16:00 UTC。
        // 即使 UTC 為週日，後端也必須以台灣時間視為週一來推算後續每週一的事件。
        var careGroupId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var groupRepository = new FakeCareGroupRepository(isMember: true);
        var seriesRepository = new FakeEventSeriesRepository(
            new EventSeries
            {
                Id = Guid.NewGuid(),
                CareGroupId = careGroupId,
                Title = "Weekly Taiwan Monday",
                StartsAt = new DateTime(2026, 4, 19, 16, 0, 0, DateTimeKind.Utc),
                RepeatPattern = EventRepeatPattern.Weekly,
                RepeatInterval = 1,
                DaysOfWeekMask = 1 << (int)DayOfWeek.Monday,
                EndType = EventSeriesEndType.Never,
                Participants = new[] { userId.ToString() },
                CreatedBy = userId.ToString()
            });
        var occurrenceRepository = new FakeEventOccurrenceRepository();
        var service = new EventOccurrenceService(seriesRepository, occurrenceRepository, groupRepository);

        var items = await service.GetOccurrencesAsync(
            userId,
            careGroupId,
            new DateTime(2026, 4, 19, 0, 0, 0, DateTimeKind.Utc),
            new DateTime(2026, 5, 10, 23, 59, 59, DateTimeKind.Utc));

        Assert.Equal(4, items.Count);
        Assert.All(items, item => Assert.Equal(DayOfWeek.Monday, item.ScheduledStartAt.DayOfWeek));
        Assert.Equal(new DateTime(2026, 4, 19, 16, 0, 0, DateTimeKind.Utc), items[0].ScheduledStartAt.UtcDateTime);
        Assert.Equal(new DateTime(2026, 4, 26, 16, 0, 0, DateTimeKind.Utc), items[1].ScheduledStartAt.UtcDateTime);
        Assert.Equal(new DateTime(2026, 5, 3, 16, 0, 0, DateTimeKind.Utc), items[2].ScheduledStartAt.UtcDateTime);
        Assert.Equal(new DateTime(2026, 5, 10, 16, 0, 0, DateTimeKind.Utc), items[3].ScheduledStartAt.UtcDateTime);
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
        Assert.Equal(new DateTime(2026, 4, 1, 8, 30, 0, DateTimeKind.Utc), items[0].ScheduledStartAt.UtcDateTime);
        Assert.Equal(new DateTime(2026, 4, 3, 8, 30, 0, DateTimeKind.Utc), items[1].ScheduledStartAt.UtcDateTime);
        Assert.Equal(new DateTime(2026, 4, 5, 8, 30, 0, DateTimeKind.Utc), items[2].ScheduledStartAt.UtcDateTime);
        Assert.Equal(new DateTime(2026, 4, 7, 8, 30, 0, DateTimeKind.Utc), items[3].ScheduledStartAt.UtcDateTime);
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
        Assert.Equal(new DateTime(2026, 1, 31, 10, 0, 0, DateTimeKind.Utc), items[0].ScheduledStartAt.UtcDateTime);
        Assert.Equal(new DateTime(2026, 2, 28, 10, 0, 0, DateTimeKind.Utc), items[1].ScheduledStartAt.UtcDateTime);
        Assert.Equal(new DateTime(2026, 3, 31, 10, 0, 0, DateTimeKind.Utc), items[2].ScheduledStartAt.UtcDateTime);
        Assert.Equal(new DateTime(2026, 4, 30, 10, 0, 0, DateTimeKind.Utc), items[3].ScheduledStartAt.UtcDateTime);
    }

    private static EventOccurrenceService BuildWeeklyService(Guid careGroupId, Guid userId, Guid seriesId, out FakeEventOccurrenceRepository occurrenceRepository)
    {
        var seriesRepository = new FakeEventSeriesRepository(
            new EventSeries
            {
                Id = seriesId,
                CareGroupId = careGroupId,
                Title = "Weekly Clinic",
                StartsAt = new DateTime(2026, 4, 6, 9, 0, 0, DateTimeKind.Utc),
                DurationMinutes = 60,
                RepeatPattern = EventRepeatPattern.Weekly,
                RepeatInterval = 1,
                DaysOfWeekMask = 1 << (int)DayOfWeek.Monday,
                EndType = EventSeriesEndType.Never,
                Participants = new[] { userId.ToString() },
                IsImportant = true,
                CreatedBy = userId.ToString()
            });
        occurrenceRepository = new FakeEventOccurrenceRepository();
        var groupRepository = new FakeCareGroupRepository(isMember: true);
        return new EventOccurrenceService(seriesRepository, occurrenceRepository, groupRepository);
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

        public Task UpdateAsync(EventSeries series) => Task.CompletedTask;

        public Task SoftDeleteAsync(EventSeries series)
        {
            series.DeletedAt = DateTime.UtcNow;
            return Task.CompletedTask;
        }

        public Task SaveChangesAsync() => Task.CompletedTask;
    }

    private sealed class FakeEventOccurrenceRepository : IEventOccurrenceRepository
    {
        public List<EventOccurrence> Items { get; } = new();

        public Task<List<EventOccurrence>> GetByRangeAsync(Guid careGroupId, DateTime from, DateTime to)
        {
            return Task.FromResult(Items
                .Where(x => x.CareGroupId == careGroupId && x.DeletedAt == null)
                .Where(x =>
                {
                    var effectiveStart = x.OverrideStartAt ?? x.OccurrenceKeyStartAt;
                    return effectiveStart >= from && effectiveStart <= to;
                })
                .ToList());
        }

        public Task<EventOccurrence?> GetBySeriesIdAndOccurrenceKeyStartAtAsync(Guid eventSeriesId, DateTime occurrenceKeyStartAt)
        {
            return Task.FromResult(Items.FirstOrDefault(x =>
                x.EventSeriesId == eventSeriesId &&
                x.OccurrenceKeyStartAt == occurrenceKeyStartAt &&
                x.DeletedAt == null));
        }

        public Task<EventOccurrence?> GetBySeriesIdAndEffectiveStartAtAsync(Guid eventSeriesId, DateTime effectiveStartAt)
        {
            return Task.FromResult(Items.FirstOrDefault(x =>
                x.EventSeriesId == eventSeriesId &&
                (x.OverrideStartAt ?? x.OccurrenceKeyStartAt) == effectiveStartAt &&
                x.DeletedAt == null));
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
        public Task<List<CareGroupMember>> GetActiveMembersWithUserAsync(Guid careGroupId) => Task.FromResult(new List<CareGroupMember>());
        public Task SaveChangesAsync() => Task.CompletedTask;
    }
}
