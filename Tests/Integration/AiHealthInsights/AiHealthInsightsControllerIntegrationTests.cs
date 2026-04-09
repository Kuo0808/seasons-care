using System.Net;
using System.Net.Http.Json;
using SeasonsCare.Api.DTOs.AiHealthInsights;
using SeasonsCare.Api.Exceptions;
using SeasonsCare.Api.Services;
using SeasonsCare.Api.Tests.Shared;
using SeasonsCare.Api.Tests.Shared.Http;

namespace SeasonsCare.Api.Tests.Integration.AiHealthInsights;

public class AiHealthInsightsControllerIntegrationTests
{
    [Fact]
    public async Task SaveInsight_ReturnsBadRequest_WhenReportTypeIsMissing()
    {
        using var factory = new StubApiFactory<IAiHealthInsightService>(new StubAiHealthInsightService());
        using var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/care-groups/aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa/ai-insights", new
        {
            dateFrom = "2026-04-01T00:00:00Z",
            dateTo = "2026-04-01T23:59:59Z",
            overallSummary = "summary",
            keyInsights = "insights",
            recommendations = "recommendations"
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        using var payload = await JsonResponseHelper.ReadJsonAsync(response);
        Assert.Equal("VALIDATION_FAILED", payload.RootElement.GetProperty("errorCode").GetString());
    }

    [Fact]
    public async Task GetLatestInsight_ReturnsNotFound_WhenServiceReturnsNoData()
    {
        using var factory = new StubApiFactory<IAiHealthInsightService>(new StubAiHealthInsightService
        {
            GetLatestException = new DomainException("not found", "NOT_FOUND", 404)
        });
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/api/care-groups/aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa/ai-insights/latest?reportType=daily");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    private sealed class StubAiHealthInsightService : IAiHealthInsightService
    {
        public Exception? GetLatestException { get; init; }

        public Task<AiHealthInsightResponse> SaveInsightAsync(Guid currentUserId, Guid careGroupId, SaveAiHealthInsightRequest request)
        {
            return Task.FromResult(new AiHealthInsightResponse
            {
                Id = Guid.NewGuid(),
                CareGroupId = careGroupId,
                ReportType = request.ReportType,
                DateFrom = request.DateFrom,
                DateTo = request.DateTo,
                OverallSummary = request.OverallSummary,
                KeyInsights = request.KeyInsights,
                Recommendations = request.Recommendations,
                GeneratedAt = DateTime.UtcNow,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                CreatedBy = currentUserId.ToString()
            });
        }

        public Task<AiHealthInsightResponse> GetLatestInsightAsync(Guid currentUserId, Guid careGroupId, string? reportType)
        {
            if (GetLatestException is not null)
            {
                throw GetLatestException;
            }

            return Task.FromResult(new AiHealthInsightResponse
            {
                Id = Guid.NewGuid(),
                CareGroupId = careGroupId,
                ReportType = reportType ?? "daily",
                DateFrom = new DateTime(2026, 4, 1, 0, 0, 0, DateTimeKind.Utc),
                DateTo = new DateTime(2026, 4, 1, 23, 59, 59, DateTimeKind.Utc),
                OverallSummary = "summary",
                KeyInsights = "insights",
                Recommendations = "recommendations",
                GeneratedAt = DateTime.UtcNow,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                CreatedBy = currentUserId.ToString()
            });
        }
    }
}
