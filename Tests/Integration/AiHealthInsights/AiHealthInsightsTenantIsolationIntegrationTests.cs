using System.Net;
using System.Net.Http.Json;
using SeasonsCare.Api.Models.Entities;
using SeasonsCare.Api.Tests.Shared;
using SeasonsCare.Api.Tests.Shared.Http;

namespace SeasonsCare.Api.Tests.Integration.AiHealthInsights;

public class AiHealthInsightsTenantIsolationIntegrationTests
{
    [Fact]
    public async Task SaveInsight_PersistsInsight_InRequestedCareGroup()
    {
        using var factory = new RealApiFactory();
        using var client = factory.Factory.CreateClient();

        var careGroup = SeedDataHelper.CreateCareGroup("Group A");
        await factory.SeedAsync(
            SeedDataHelper.CreateUser(),
            careGroup,
            SeedDataHelper.CreateMember(careGroup.Id));

        var response = await client.PostAsJsonAsync($"/api/care-groups/{careGroup.Id}/ai-insights", new
        {
            reportType = "daily",
            dateFrom = "2026-04-01T00:00:00Z",
            dateTo = "2026-04-01T23:59:59Z",
            overallSummary = "summary",
            keyInsights = "insights",
            recommendations = "recommendations",
            sourceDataHash = "hash-1",
            modelName = "gpt-5",
            promptVersion = "v1"
        });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        using var payload = await JsonResponseHelper.ReadJsonAsync(response);
        var createdId = payload.RootElement.GetProperty("data").GetProperty("id").GetGuid();
        var saved = await factory.FindAsync<AiHealthInsight>(createdId);

        Assert.NotNull(saved);
        Assert.Equal(careGroup.Id, saved!.CareGroupId);
        Assert.Equal("daily", saved.ReportType);
        Assert.Equal("summary", saved.OverallSummary);
        Assert.Equal("hash-1", saved.SourceDataHash);
    }

    [Fact]
    public async Task GetLatestInsight_ReturnsLatestInsight_ForRequestedCareGroupAndReportType()
    {
        using var factory = new RealApiFactory();
        using var client = factory.Factory.CreateClient();

        var careGroupA = SeedDataHelper.CreateCareGroup("Group A");
        var careGroupB = SeedDataHelper.CreateCareGroup("Group B");
        var insightAOld = SeedDataHelper.CreateAiHealthInsight(careGroupA.Id, "daily", new DateTime(2026, 4, 8, 8, 0, 0, DateTimeKind.Utc));
        insightAOld.OverallSummary = "old summary";
        var insightANew = SeedDataHelper.CreateAiHealthInsight(careGroupA.Id, "daily", new DateTime(2026, 4, 9, 8, 0, 0, DateTimeKind.Utc));
        insightANew.OverallSummary = "new summary";
        var insightB = SeedDataHelper.CreateAiHealthInsight(careGroupB.Id, "daily", new DateTime(2026, 4, 10, 8, 0, 0, DateTimeKind.Utc));
        insightB.OverallSummary = "group b summary";

        await factory.SeedAsync(
            SeedDataHelper.CreateUser(),
            careGroupA,
            careGroupB,
            SeedDataHelper.CreateMember(careGroupA.Id),
            SeedDataHelper.CreateMember(careGroupB.Id),
            insightAOld,
            insightANew,
            insightB);

        var response = await client.GetAsync($"/api/care-groups/{careGroupA.Id}/ai-insights/latest?reportType=daily");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        using var payload = await JsonResponseHelper.ReadJsonAsync(response);
        var data = payload.RootElement.GetProperty("data");
        Assert.Equal(insightANew.Id, data.GetProperty("id").GetGuid());
        Assert.Equal("new summary", data.GetProperty("overallSummary").GetString());
    }

    [Fact]
    public async Task SaveInsight_UpsertsExistingInsight_WhenSameRangeAndReportType()
    {
        using var factory = new RealApiFactory();
        using var client = factory.Factory.CreateClient();

        var careGroup = SeedDataHelper.CreateCareGroup("Group A");
        var existing = SeedDataHelper.CreateAiHealthInsight(careGroup.Id, "daily", new DateTime(2026, 4, 9, 8, 0, 0, DateTimeKind.Utc));
        existing.DateFrom = new DateTime(2026, 4, 1, 0, 0, 0, DateTimeKind.Utc);
        existing.DateTo = new DateTime(2026, 4, 1, 23, 59, 59, DateTimeKind.Utc);
        existing.OverallSummary = "old summary";

        await factory.SeedAsync(
            SeedDataHelper.CreateUser(),
            careGroup,
            SeedDataHelper.CreateMember(careGroup.Id),
            existing);

        var response = await client.PostAsJsonAsync($"/api/care-groups/{careGroup.Id}/ai-insights", new
        {
            reportType = "daily",
            dateFrom = "2026-04-01T00:00:00Z",
            dateTo = "2026-04-01T23:59:59Z",
            overallSummary = "new summary",
            keyInsights = "new insights",
            recommendations = "new recommendations"
        });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var allInsights = await factory.GetAiHealthInsightsAsync();
        Assert.Single(allInsights);
        Assert.Equal("new summary", allInsights[0].OverallSummary);
        Assert.Equal("new insights", allInsights[0].KeyInsights);
    }
}
