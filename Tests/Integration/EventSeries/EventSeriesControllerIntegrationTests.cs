using System.Net;
using System.Net.Http.Json;
using SeasonsCare.Api.DTOs.Common;
using SeasonsCare.Api.DTOs.EventSeries;
using SeasonsCare.Api.Models.Enums;
using SeasonsCare.Api.Services;
using SeasonsCare.Api.Tests.Shared;
using SeasonsCare.Api.Tests.Shared.Http;

namespace SeasonsCare.Api.Tests.Integration.EventSeries;

public class EventSeriesControllerIntegrationTests
{
    [Fact]
    public async Task CreateSeries_AcceptsWeeklyDayString_AndReturnsWeeklyDayString()
    {
        using var factory = new StubApiFactory<IEventSeriesService>(new StubEventSeriesService());
        using var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/care-groups/aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa/event-series", new
        {
            title = "Weekly Visit",
            startsAt = "2026-04-13T09:00:00Z",
            repeatPattern = "weeklyDay",
            repeatInterval = 1,
            daysOfWeek = new[] { 1 },
            endType = 0
        });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        using var payload = await JsonResponseHelper.ReadJsonAsync(response);
        Assert.Equal("weeklyDay", payload.RootElement.GetProperty("data").GetProperty("repeatPattern").GetString());
    }

    [Fact]
    public async Task CreateSeries_AcceptsMonthlyString()
    {
        using var factory = new StubApiFactory<IEventSeriesService>(new StubEventSeriesService());
        using var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/care-groups/aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa/event-series", new
        {
            title = "Monthly Review",
            startsAt = "2026-04-13T09:00:00Z",
            repeatPattern = "monthly",
            repeatInterval = 1,
            endType = 0
        });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        using var payload = await JsonResponseHelper.ReadJsonAsync(response);
        Assert.Equal("monthly", payload.RootElement.GetProperty("data").GetProperty("repeatPattern").GetString());
    }

    private sealed class StubEventSeriesService : IEventSeriesService
    {
        public Task<PagedResponse<EventSeriesResponse>> GetSeriesAsync(Guid currentUserId, Guid careGroupId, PaginationRequest pagination)
            => throw new NotImplementedException();

        public Task<IReadOnlyList<EventSeriesResponse>> GetAllSeriesAsync(Guid currentUserId, Guid careGroupId)
            => throw new NotImplementedException();

        public Task<EventSeriesResponse> GetSeriesByIdAsync(Guid currentUserId, Guid careGroupId, Guid seriesId)
            => throw new NotImplementedException();

        public Task<EventSeriesResponse> CreateSeriesAsync(Guid currentUserId, Guid careGroupId, CreateEventSeriesRequest request)
        {
            return Task.FromResult(new EventSeriesResponse
            {
                Id = Guid.NewGuid(),
                CareGroupId = careGroupId,
                Title = request.Title,
                StartsAt = request.StartsAt,
                RepeatPattern = request.RepeatPattern,
                RepeatInterval = request.RepeatInterval,
                DaysOfWeek = request.DaysOfWeek ?? new List<DayOfWeek>(),
                EndType = request.EndType,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                CreatedBy = currentUserId.ToString()
            });
        }

        public Task<EventSeriesResponse> UpdateSeriesAsync(Guid currentUserId, Guid careGroupId, Guid seriesId, UpdateEventSeriesRequest request)
            => throw new NotImplementedException();

        public Task DeleteSeriesAsync(Guid currentUserId, Guid careGroupId, Guid seriesId)
            => throw new NotImplementedException();
    }
}
