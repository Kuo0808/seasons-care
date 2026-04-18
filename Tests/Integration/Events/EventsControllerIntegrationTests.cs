using System.Net;
using System.Net.Http.Json;
using SeasonsCare.Api.DTOs.Events;
using SeasonsCare.Api.Models.Enums;
using SeasonsCare.Api.Services;
using SeasonsCare.Api.Tests.Shared;
using SeasonsCare.Api.Tests.Shared.Http;

namespace SeasonsCare.Api.Tests.Integration.Events;

public class EventsControllerIntegrationTests
{
    private const string CareGroupIdPath = "aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa";
    private static readonly Guid EventSeriesId = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");

    // FME-1
    [Fact]
    public async Task CreateEvent_Returns201_AndPassesFieldSetToFacade()
    {
        var stub = new StubEventFacadeService();
        using var factory = new StubApiFactory<IEventFacadeService>(stub);
        using var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync($"/api/care-groups/{CareGroupIdPath}/events", new
        {
            title = "每週陪診",
            description = "記得帶健保卡",
            startsAt = "2026-04-20T09:00:00Z",
            repeatPattern = "weeklyDay",
            daysOfWeek = new[] { 1 },
            participants = new[] { "user-a" },
            isImportant = true
        });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        using var payload = await JsonResponseHelper.ReadJsonAsync(response);
        var data = payload.RootElement.GetProperty("data");
        Assert.Equal("每週陪診", data.GetProperty("title").GetString());
        Assert.Equal("記得帶健保卡", data.GetProperty("description").GetString());
        Assert.Equal("weeklyDay", data.GetProperty("repeatPattern").GetString());
        Assert.True(data.GetProperty("isImportant").GetBoolean());

        Assert.NotNull(stub.LastCreate);
        Assert.Equal(EventRepeatPattern.Weekly, stub.LastCreate!.RepeatPattern);
        Assert.Contains(DayOfWeek.Monday, stub.LastCreate.DaysOfWeek ?? new List<DayOfWeek>());
    }

    // FME-2
    [Fact]
    public async Task GetEvents_ReturnsOccurrenceList_WithStatusAndRepeatPatternStrings()
    {
        var stub = new StubEventFacadeService();
        using var factory = new StubApiFactory<IEventFacadeService>(stub);
        using var client = factory.CreateClient();

        var response = await client.GetAsync(
            $"/api/care-groups/{CareGroupIdPath}/events?from=2026-04-01T00:00:00Z&to=2026-04-30T23:59:59Z");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        using var payload = await JsonResponseHelper.ReadJsonAsync(response);
        var first = payload.RootElement.GetProperty("data")[0];
        Assert.Equal("pending", first.GetProperty("status").GetString());
        Assert.Equal("weeklyDay", first.GetProperty("repeatPattern").GetString());
    }

    [Fact]
    public async Task GetEvents_Returns400_WhenFromAndToAreMissing()
    {
        var stub = new StubEventFacadeService();
        using var factory = new StubApiFactory<IEventFacadeService>(stub);
        using var client = factory.CreateClient();

        var response = await client.GetAsync($"/api/care-groups/{CareGroupIdPath}/events");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    // FME-3
    [Fact]
    public async Task UpdateEvent_Returns200_AndPassesFieldSetToFacade()
    {
        var stub = new StubEventFacadeService();
        using var factory = new StubApiFactory<IEventFacadeService>(stub);
        using var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync(
            $"/api/care-groups/{CareGroupIdPath}/events/{EventSeriesId}", new
            {
                title = "每月家庭會議",
                description = "線上會議",
                startsAt = "2026-05-01T09:00:00Z",
                repeatPattern = "monthly",
                isImportant = false
            });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        using var payload = await JsonResponseHelper.ReadJsonAsync(response);
        Assert.Equal("monthly", payload.RootElement.GetProperty("data").GetProperty("repeatPattern").GetString());
        Assert.Equal(EventSeriesId, stub.LastUpdateEventId);
        Assert.Equal(EventRepeatPattern.Monthly, stub.LastUpdate!.RepeatPattern);
    }

    // FME-4
    [Fact]
    public async Task DeleteEvent_Returns200_AndInvokesFacade()
    {
        var stub = new StubEventFacadeService();
        using var factory = new StubApiFactory<IEventFacadeService>(stub);
        using var client = factory.CreateClient();

        var response = await client.DeleteAsync($"/api/care-groups/{CareGroupIdPath}/events/{EventSeriesId}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(EventSeriesId, stub.LastDeleteEventId);
    }

    // FME-5 — 純 status 切換
    [Theory]
    [InlineData("completed")]
    [InlineData("cancelled")]
    [InlineData("pending")]
    public async Task UpdateInstance_Returns200_ForAllStatusStrings(string status)
    {
        var stub = new StubEventFacadeService();
        using var factory = new StubApiFactory<IEventFacadeService>(stub);
        using var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync(
            $"/api/care-groups/{CareGroupIdPath}/events/{EventSeriesId}/status", new
            {
                scheduledAt = "2026-04-20T09:00:00Z",
                status
            });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(status, stub.LastInstanceRequest!.Status);
    }

    // FME-5 — 含 description / participants
    [Fact]
    public async Task UpdateInstance_Returns200_WhenBodyContainsDescriptionAndParticipants()
    {
        var stub = new StubEventFacadeService();
        using var factory = new StubApiFactory<IEventFacadeService>(stub);
        using var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync(
            $"/api/care-groups/{CareGroupIdPath}/events/{EventSeriesId}/status", new
            {
                scheduledAt = "2026-04-20T09:00:00Z",
                description = "臨時改地點",
                participants = new[] { "user-a", "user-b" }
            });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("臨時改地點", stub.LastInstanceRequest!.Description);
        Assert.Equal(2, stub.LastInstanceRequest.Participants!.Count);
    }

    // FME-6
    [Fact]
    public async Task GetInstanceStatus_Returns200_AndReturnsItem()
    {
        var stub = new StubEventFacadeService();
        using var factory = new StubApiFactory<IEventFacadeService>(stub);
        using var client = factory.CreateClient();

        var response = await client.GetAsync(
            $"/api/care-groups/{CareGroupIdPath}/events/{EventSeriesId}/status?scheduledAt=2026-04-20T09:00:00Z");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        using var payload = await JsonResponseHelper.ReadJsonAsync(response);
        var data = payload.RootElement.GetProperty("data");
        Assert.Equal(EventSeriesId, data.GetProperty("eventSeriesId").GetGuid());
        Assert.Equal("pending", data.GetProperty("status").GetString());
        Assert.Equal("weeklyDay", data.GetProperty("repeatPattern").GetString());
    }

    private sealed class StubEventFacadeService : IEventFacadeService
    {
        public CreateEventRequest? LastCreate { get; private set; }
        public UpdateEventRequest? LastUpdate { get; private set; }
        public Guid? LastUpdateEventId { get; private set; }
        public Guid? LastDeleteEventId { get; private set; }
        public UpdateInstanceRequest? LastInstanceRequest { get; private set; }

        public Task<EventResponse> CreateEventAsync(Guid currentUserId, Guid careGroupId, CreateEventRequest request)
        {
            LastCreate = request;
            return Task.FromResult(new EventResponse
            {
                Id = EventSeriesId,
                Title = request.Title,
                Description = request.Description,
                StartsAt = request.StartsAt,
                RepeatPattern = request.RepeatPattern,
                DaysOfWeek = request.DaysOfWeek ?? new List<DayOfWeek>(),
                Participants = request.Participants ?? new List<string>(),
                IsImportant = request.IsImportant,
                CareGroupId = careGroupId,
                CreatedAt = DateTimeOffset.UtcNow,
                UpdatedAt = DateTimeOffset.UtcNow,
                CreatedBy = currentUserId.ToString()
            });
        }

        public Task<IReadOnlyList<EventOccurrenceItem>> GetEventsAsync(Guid currentUserId, Guid careGroupId, GetEventsRequest request)
        {
            IReadOnlyList<EventOccurrenceItem> items =
            [
                new EventOccurrenceItem
                {
                    EventSeriesId = EventSeriesId,
                    Title = "每週陪診",
                    ScheduledAt = new DateTimeOffset(2026, 4, 20, 9, 0, 0, TimeSpan.Zero),
                    Status = "pending",
                    RepeatPattern = EventRepeatPattern.Weekly
                }
            ];
            return Task.FromResult(items);
        }

        public Task<EventResponse> UpdateEventAsync(Guid currentUserId, Guid careGroupId, Guid eventId, UpdateEventRequest request)
        {
            LastUpdateEventId = eventId;
            LastUpdate = request;
            return Task.FromResult(new EventResponse
            {
                Id = eventId,
                Title = request.Title,
                Description = request.Description,
                StartsAt = request.StartsAt,
                RepeatPattern = request.RepeatPattern,
                DaysOfWeek = request.DaysOfWeek ?? new List<DayOfWeek>(),
                Participants = request.Participants ?? new List<string>(),
                IsImportant = request.IsImportant,
                CareGroupId = careGroupId,
                CreatedAt = DateTimeOffset.UtcNow,
                UpdatedAt = DateTimeOffset.UtcNow,
                CreatedBy = currentUserId.ToString()
            });
        }

        public Task DeleteEventAsync(Guid currentUserId, Guid careGroupId, Guid eventId)
        {
            LastDeleteEventId = eventId;
            return Task.CompletedTask;
        }

        public Task UpdateInstanceAsync(Guid currentUserId, Guid careGroupId, Guid eventId, UpdateInstanceRequest request)
        {
            LastInstanceRequest = request;
            return Task.CompletedTask;
        }

        public Task<EventOccurrenceItem> GetInstanceStatusAsync(Guid currentUserId, Guid careGroupId, Guid eventId, DateTimeOffset scheduledAt)
        {
            return Task.FromResult(new EventOccurrenceItem
            {
                EventSeriesId = eventId,
                Title = "每週陪診",
                ScheduledAt = scheduledAt,
                Status = "pending",
                RepeatPattern = EventRepeatPattern.Weekly
            });
        }
    }
}
