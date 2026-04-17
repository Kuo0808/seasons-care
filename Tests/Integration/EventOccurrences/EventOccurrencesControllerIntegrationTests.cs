using System.Net;
using System.Net.Http.Json;
using SeasonsCare.Api.DTOs.EventOccurrences;
using SeasonsCare.Api.Models.Enums;
using SeasonsCare.Api.Services;
using SeasonsCare.Api.Tests.Shared;
using SeasonsCare.Api.Tests.Shared.Http;

namespace SeasonsCare.Api.Tests.Integration.EventOccurrences;

public class EventOccurrencesControllerIntegrationTests
{
    [Fact]
    public async Task GetOccurrences_ReturnsPendingStatusString()
    {
        using var factory = new StubApiFactory<IEventOccurrenceService>(new StubEventOccurrenceService());
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/api/care-groups/aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa/event-occurrences?from=2026-04-01T00:00:00Z&to=2026-04-30T23:59:59Z");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        using var payload = await JsonResponseHelper.ReadJsonAsync(response);
        Assert.Equal("pending", payload.RootElement.GetProperty("data")[0].GetProperty("status").GetString());
    }

    [Fact]
    public async Task CompleteOccurrence_ReturnsOk()
    {
        using var factory = new StubApiFactory<IEventOccurrenceService>(new StubEventOccurrenceService());
        using var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/care-groups/aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa/event-occurrences/complete", new
        {
            eventSeriesId = "bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb",
            scheduledStartAt = "2026-04-08T15:00:00Z"
        });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task UpdateOccurrence_ReturnsOk()
    {
        using var factory = new StubApiFactory<IEventOccurrenceService>(new StubEventOccurrenceService());
        using var client = factory.CreateClient();

        var response = await client.PatchAsJsonAsync("/api/care-groups/aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa/event-occurrences", new
        {
            eventSeriesId = "bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb",
            scheduledStartAt = "2026-04-08T15:00:00Z",
            title = "Updated Reminder"
        });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task ClearOccurrenceOverride_ReturnsOk()
    {
        using var factory = new StubApiFactory<IEventOccurrenceService>(new StubEventOccurrenceService());
        using var client = factory.CreateClient();

        var request = new HttpRequestMessage(HttpMethod.Delete, "/api/care-groups/aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa/event-occurrences/override")
        {
            Content = JsonContent.Create(new
            {
                eventSeriesId = "bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb",
                scheduledStartAt = "2026-04-08T15:00:00Z"
            })
        };

        var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    private sealed class StubEventOccurrenceService : IEventOccurrenceService
    {
        public Task<IReadOnlyList<EventOccurrenceResponse>> GetOccurrencesAsync(Guid currentUserId, Guid careGroupId, DateTime from, DateTime to)
        {
            IReadOnlyList<EventOccurrenceResponse> response =
            [
                new EventOccurrenceResponse
                {
                    EventSeriesId = Guid.NewGuid(),
                    Title = "Medication Reminder",
                    ScheduledStartAt = new DateTime(2026, 4, 8, 15, 0, 0, DateTimeKind.Utc),
                    Status = EventOccurrenceStatus.Scheduled
                }
            ];

            return Task.FromResult(response);
        }

        public Task CancelOccurrenceAsync(Guid currentUserId, Guid careGroupId, Guid eventSeriesId, DateTime scheduledStartAt)
            => Task.CompletedTask;

        public Task CompleteOccurrenceAsync(Guid currentUserId, Guid careGroupId, Guid eventSeriesId, DateTime scheduledStartAt)
            => Task.CompletedTask;

        public Task<EventOccurrenceResponse> UpdateOccurrenceAsync(Guid currentUserId, Guid careGroupId, UpdateEventOccurrenceRequest request)
        {
            return Task.FromResult(new EventOccurrenceResponse
            {
                EventSeriesId = request.EventSeriesId,
                Title = request.Title ?? "Medication Reminder",
                ScheduledStartAt = request.StartsAt ?? request.ScheduledStartAt,
                ScheduledEndAt = request.EndsAt,
                Participants = request.Participants ?? new List<string>(),
                Status = request.Status ?? EventOccurrenceStatus.Scheduled,
                IsImportant = request.IsImportant ?? false,
                HasOverrides = true
            });
        }

        public Task ClearOccurrenceOverrideAsync(Guid currentUserId, Guid careGroupId, Guid eventSeriesId, DateTime scheduledStartAt)
            => Task.CompletedTask;
    }
}
