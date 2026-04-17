using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using SeasonsCare.Api.Config;
using SeasonsCare.Api.Data;
using SeasonsCare.Api.DTOs.HealthDashboard;
using SeasonsCare.Api.Models.Entities;
using SeasonsCare.Api.Services.AI;
using SeasonsCare.Api.Tests.Shared;
using SeasonsCare.Api.Tests.Shared.Http;

namespace SeasonsCare.Api.Tests.Integration.HealthDashboard;

public class HealthDashboardIntegrationTests
{
    // ──────────────────────────────────────────────
    // API 1：weekly-insight
    // ──────────────────────────────────────────────

    [Fact]
    public async Task GetWeeklyInsight_ReturnsCachedInsight_WithoutCallingAi()
    {
        var fakeAiService = new FakeAiIntegrationService();
        using var factory = new HealthDashboardApiFactory(fakeAiService);
        using var client = factory.Factory.CreateClient();

        var careGroup = SeedDataHelper.CreateCareGroup("Group A");
        var (dateFrom, dateTo) = GetDashboardRange();
        var cachedInsight = SeedDataHelper.CreateAiHealthInsight(careGroup.Id, "health_dashboard_7d", DateTime.UtcNow);
        cachedInsight.DateFrom = dateFrom;
        cachedInsight.DateTo = dateTo;
        cachedInsight.OverallSummary = "近 7 天整體狀態穩定";
        cachedInsight.TodaySummary = "今天已完成血壓量測";
        cachedInsight.KeyInsights = "今日量測可與本週趨勢一起判讀";
        cachedInsight.Recommendations = "維持固定時段量測，並記錄飲食變化。";

        await factory.SeedAsync(
            SeedDataHelper.CreateUser(),
            careGroup,
            SeedDataHelper.CreateMember(careGroup.Id),
            cachedInsight);

        var response = await client.GetAsync($"/api/care-groups/{careGroup.Id}/health-dashboard/weekly-insight");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        using var payload = await JsonResponseHelper.ReadJsonAsync(response);
        var data = payload.RootElement.GetProperty("data");

        Assert.True(data.GetProperty("isFromCache").GetBoolean());
        // 快取回來時 heroReport 會使用 fallback（因為 DB 沒存結構化欄位）
        Assert.NotEmpty(data.GetProperty("heroReport").GetProperty("headline").GetString()!);
        Assert.NotEmpty(data.GetProperty("keyInsightSection").GetProperty("label").GetString()!);
        Assert.NotEmpty(data.GetProperty("actionSuggestionSection").GetProperty("label").GetString()!);
        Assert.Equal(0, fakeAiService.CallCount);
    }

    [Fact]
    public async Task GetWeeklyInsight_ReturnsStructuredSections_WhenCacheMiss()
    {
        var fakeAiService = new FakeAiIntegrationService();
        using var factory = new HealthDashboardApiFactory(fakeAiService);
        using var client = factory.Factory.CreateClient();

        var careGroup = SeedDataHelper.CreateCareGroup("Group A");
        var (dateFrom, _) = GetDashboardRange();

        await factory.SeedAsync(
            SeedDataHelper.CreateUser(),
            careGroup,
            SeedDataHelper.CreateMember(careGroup.Id),
            SeedDataHelper.CreateBloodPressure(careGroup.Id, 130, 86, dateFrom.AddDays(6).AddHours(2)),
            SeedDataHelper.CreateBloodSugar(careGroup.Id, 118m, "飯後", dateFrom.AddDays(6).AddHours(3)));

        var response = await client.GetAsync($"/api/care-groups/{careGroup.Id}/health-dashboard/weekly-insight");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        using var payload = await JsonResponseHelper.ReadJsonAsync(response);
        var data = payload.RootElement.GetProperty("data");

        Assert.False(data.GetProperty("isFromCache").GetBoolean());
        Assert.Equal("本週健康趨勢大致穩定", data.GetProperty("heroReport").GetProperty("headline").GetString());
        Assert.Equal("關鍵數據洞察", data.GetProperty("keyInsightSection").GetProperty("label").GetString());
        Assert.Equal("健康行動建議", data.GetProperty("actionSuggestionSection").GetProperty("label").GetString());
        Assert.Equal("health-report-v1", data.GetProperty("meta").GetProperty("rulesVersion").GetString());
        Assert.Equal(1, fakeAiService.CallCount);
        Assert.NotNull(fakeAiService.LastInput);
        Assert.Contains("目前只有 1 筆血壓", fakeAiService.LastInput!.BloodPressureSummary);

        var insights = await factory.GetAiHealthInsightsAsync();
        Assert.Single(insights);
        Assert.Equal("health_dashboard_7d", insights[0].ReportType);
    }

    [Fact]
    public async Task GetWeeklyInsight_ReturnsFallback_WhenAiFails()
    {
        var fakeAiService = new FakeAiIntegrationService { ShouldThrow = true };
        using var factory = new HealthDashboardApiFactory(fakeAiService);
        using var client = factory.Factory.CreateClient();

        var careGroup = SeedDataHelper.CreateCareGroup("Group A");

        await factory.SeedAsync(
            SeedDataHelper.CreateUser(),
            careGroup,
            SeedDataHelper.CreateMember(careGroup.Id));

        var response = await client.GetAsync($"/api/care-groups/{careGroup.Id}/health-dashboard/weekly-insight");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        using var payload = await JsonResponseHelper.ReadJsonAsync(response);
        var data = payload.RootElement.GetProperty("data");

        Assert.True(data.GetProperty("meta").GetProperty("isFallback").GetBoolean());
        Assert.NotEmpty(data.GetProperty("heroReport").GetProperty("headline").GetString()!);
    }

    // ──────────────────────────────────────────────
    // API 2：today-insight（不呼叫 AI）
    // ──────────────────────────────────────────────

    [Fact]
    public async Task GetTodayInsight_ReturnsCards_WhenNoTodayRecords()
    {
        var fakeAiService = new FakeAiIntegrationService();
        using var factory = new HealthDashboardApiFactory(fakeAiService);
        using var client = factory.Factory.CreateClient();

        var careGroup = SeedDataHelper.CreateCareGroup("Group A");
        await factory.SeedAsync(
            SeedDataHelper.CreateUser(),
            careGroup,
            SeedDataHelper.CreateMember(careGroup.Id));

        var response = await client.GetAsync($"/api/care-groups/{careGroup.Id}/health-dashboard/today-insight");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        using var payload = await JsonResponseHelper.ReadJsonAsync(response);
        var data = payload.RootElement.GetProperty("data");

        Assert.False(data.GetProperty("hasTodayRecords").GetBoolean());
        Assert.Equal(0, data.GetProperty("recordCount").GetInt32());
        Assert.Equal("AI 分析摘要", data.GetProperty("cards")[0].GetProperty("title").GetString());
        Assert.Equal("low", data.GetProperty("meta").GetProperty("confidence").GetString());
        // 不應呼叫 AI
        Assert.Equal(0, fakeAiService.CallCount);
    }

    [Fact]
    public async Task GetTodayInsight_ReturnsLatestMetrics_WhenTodayHasRecords()
    {
        var fakeAiService = new FakeAiIntegrationService();
        using var factory = new HealthDashboardApiFactory(fakeAiService);
        using var client = factory.Factory.CreateClient();

        var careGroup = SeedDataHelper.CreateCareGroup("Group A");
        var todayStart = NormalizeTimestamp(TimeHelper.GetTaiwanDateStartUtc());

        await factory.SeedAsync(
            SeedDataHelper.CreateUser(),
            careGroup,
            SeedDataHelper.CreateMember(careGroup.Id),
            SeedDataHelper.CreateBloodPressure(careGroup.Id, 125, 82, todayStart.AddHours(8)));

        var response = await client.GetAsync($"/api/care-groups/{careGroup.Id}/health-dashboard/today-insight");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        using var payload = await JsonResponseHelper.ReadJsonAsync(response);
        var data = payload.RootElement.GetProperty("data");
        var cards = data.GetProperty("cards");

        Assert.True(data.GetProperty("hasTodayRecords").GetBoolean());
        Assert.Equal(1, data.GetProperty("recordCount").GetInt32());
        Assert.True(cards.GetArrayLength() >= 2);
        Assert.Contains("先不用太緊張", cards[0].GetProperty("summary").GetString());
        Assert.Contains("建議", cards[1].GetProperty("progressNote").GetString());
        Assert.Contains("血壓", cards[1].GetProperty("title").GetString());
        // 不應呼叫 AI
        Assert.Equal(0, fakeAiService.CallCount);
    }

    // ──────────────────────────────────────────────
    // API 3：trend-overview（不呼叫 AI）
    // ──────────────────────────────────────────────

    [Fact]
    public async Task GetTrendOverview_ReturnsMetricCardsWithChartPoints()
    {
        var fakeAiService = new FakeAiIntegrationService();
        using var factory = new HealthDashboardApiFactory(fakeAiService);
        using var client = factory.Factory.CreateClient();

        var careGroup = SeedDataHelper.CreateCareGroup("Group A");
        var (dateFrom, _) = GetDashboardRange();

        await factory.SeedAsync(
            SeedDataHelper.CreateUser(),
            careGroup,
            SeedDataHelper.CreateMember(careGroup.Id),
            SeedDataHelper.CreateBloodPressure(careGroup.Id, 120, 80, dateFrom.AddHours(2)),
            SeedDataHelper.CreateBloodPressure(careGroup.Id, 140, 88, dateFrom.AddDays(6).AddHours(1)),
            SeedDataHelper.CreateBloodSugar(careGroup.Id, 150m, "飯後", dateFrom.AddHours(4)),
            SeedDataHelper.CreateBloodSugar(careGroup.Id, 130m, "飯後", dateFrom.AddDays(6).AddHours(4)),
            SeedDataHelper.CreateWeight(careGroup.Id, 70.0m, dateFrom.AddDays(2).AddHours(4)),
            SeedDataHelper.CreateWeight(careGroup.Id, 70.2m, dateFrom.AddDays(6).AddHours(6)));

        var response = await client.GetAsync($"/api/care-groups/{careGroup.Id}/health-dashboard/trend-overview");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        using var payload = await JsonResponseHelper.ReadJsonAsync(response);
        var metrics = payload.RootElement.GetProperty("data").GetProperty("metrics");

        Assert.Equal(5, metrics.GetArrayLength());

        var bloodPressure = metrics.EnumerateArray().First(
            x => x.GetProperty("metricType").GetString() == "blood_pressure");
        Assert.Equal("130 / 84", bloodPressure.GetProperty("displayValue").GetString());
        Assert.Equal("140/88", bloodPressure.GetProperty("latestValue").GetString());
        Assert.Equal(7, bloodPressure.GetProperty("points").GetArrayLength());
        Assert.Equal(7, bloodPressure.GetProperty("secondaryPoints").GetArrayLength());

        var weight = metrics.EnumerateArray().First(
            x => x.GetProperty("metricType").GetString() == "weight");
        Assert.Equal("70.1", weight.GetProperty("displayValue").GetString());
        Assert.Contains("體重", weight.GetProperty("summary").GetString());

        // 不應呼叫 AI
        Assert.Equal(0, fakeAiService.CallCount);
    }

    [Fact]
    public async Task GetTrendOverview_UsesRuleBasedLabels_WhenNoCachedAi()
    {
        var fakeAiService = new FakeAiIntegrationService();
        using var factory = new HealthDashboardApiFactory(fakeAiService);
        using var client = factory.Factory.CreateClient();

        var careGroup = SeedDataHelper.CreateCareGroup("Group A");
        var (dateFrom, _) = GetDashboardRange();

        await factory.SeedAsync(
            SeedDataHelper.CreateUser(),
            careGroup,
            SeedDataHelper.CreateMember(careGroup.Id),
            SeedDataHelper.CreateBloodPressure(careGroup.Id, 120, 78, dateFrom.AddHours(2)));

        var response = await client.GetAsync($"/api/care-groups/{careGroup.Id}/health-dashboard/trend-overview");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        using var payload = await JsonResponseHelper.ReadJsonAsync(response);
        var metrics = payload.RootElement.GetProperty("data").GetProperty("metrics");

        var bp = metrics.EnumerateArray().First(
            x => x.GetProperty("metricType").GetString() == "blood_pressure");
        Assert.Equal("穩定", bp.GetProperty("statusLabel").GetString());

        var oxygen = metrics.EnumerateArray().First(
            x => x.GetProperty("metricType").GetString() == "blood_oxygen");
        Assert.Equal("累積中", oxygen.GetProperty("statusLabel").GetString());
    }

    // ──────────────────────────────────────────────
    // Helper
    // ──────────────────────────────────────────────

    private static (DateTime DateFrom, DateTime DateTo) GetDashboardRange()
    {
        var todayStart = NormalizeTimestamp(TimeHelper.GetTaiwanDateStartUtc());
        return (
            NormalizeTimestamp(todayStart.AddDays(-6)),
            NormalizeTimestamp(todayStart.AddDays(1).AddMilliseconds(-1)));
    }

    private static DateTime NormalizeTimestamp(DateTime value)
    {
        var utcValue = value.Kind == DateTimeKind.Utc ? value : value.ToUniversalTime();
        return new DateTime(utcValue.Ticks - (utcValue.Ticks % TimeSpan.TicksPerMillisecond), DateTimeKind.Utc);
    }

    // ──────────────────────────────────────────────
    // Fake AI Service
    // ──────────────────────────────────────────────

    private sealed class FakeAiIntegrationService : IAiIntegrationService
    {
        public int CallCount { get; private set; }
        public bool ShouldThrow { get; set; }
        public HealthInsightPromptInput? LastInput { get; private set; }

        public Task<AiGeneratedInsightDto> GenerateHealthInsightAsync(HealthInsightPromptInput input)
        {
            CallCount++;
            LastInput = input;

            if (ShouldThrow)
            {
                throw new InvalidOperationException("Simulated AI failure for testing.");
            }

            return Task.FromResult(new AiGeneratedInsightDto
            {
                OverallSummary = "本週健康趨勢大致穩定",
                TodaySummary = "今日量測完成，可持續觀察後續變化。",
                KeyInsights = "今日量測有助於補齊本週趨勢",
                Recommendations = "維持固定時段量測，並搭配飲食紀錄一起觀察。",
                HeroReport = new HealthDashboardHeroReportDto
                {
                    Headline = "本週健康趨勢大致穩定",
                    Body = "今天的量測資料已補齊本週觀察脈絡，適合持續追蹤。",
                    Tone = "supportive",
                    Confidence = "medium"
                },
                KeyInsightSection = new HealthDashboardInsightSectionDto
                {
                    Label = "關鍵數據洞察",
                    Body = "今天新增的量測資料讓近 7 天趨勢更容易判讀。",
                    MetricType = "blood_pressure",
                    Severity = "low"
                },
                ActionSuggestionSection = new HealthDashboardActionSectionDto
                {
                    Label = "健康行動建議",
                    Body = "維持固定時段量測，並搭配飲食紀錄一起觀察。",
                    Priority = "medium",
                    Timeframe = "today"
                },
                TodayCards = new List<HealthDashboardTodayCardDto>
                {
                    new()
                    {
                        Title = "AI 分析摘要",
                        Summary = "今天的量測已補齊本週觀察脈絡。",
                        ProgressNote = "今日健康任務達成 40%",
                        IconType = "insight",
                        Tone = "supportive"
                    }
                },
                TrendLabels = new TrendLabelsDto
                {
                    BloodPressure = "需觀察",
                    BloodOxygen = "資料不足",
                    BloodSugar = "穩定",
                    Temperature = "資料不足",
                    Weight = "穩定"
                },
                Alerts = new List<HealthDashboardAlertDto>(),
                SourceDataHash = "default-hash",
                ModelName = "gpt-test",
                PromptVersion = "test-v1",
                GeneratedAt = DateTime.UtcNow
            });
        }
    }

    // ──────────────────────────────────────────────
    // Test Factory
    // ──────────────────────────────────────────────

    private sealed class HealthDashboardApiFactory : IDisposable
    {
        private const string TestJwtSecret = "test-secret-key-that-is-at-least-32-chars";
        private readonly SqliteConnection _connection;

        public WebApplicationFactory<Program> Factory { get; }

        public HealthDashboardApiFactory(IAiIntegrationService aiIntegrationService)
        {
            _connection = new SqliteConnection("DataSource=:memory:");
            _connection.Open();

            Factory = new WebApplicationFactory<Program>()
                .WithWebHostBuilder(builder =>
                {
                    builder.UseEnvironment("Testing");

                    builder.ConfigureAppConfiguration((_, config) =>
                    {
                        config.AddInMemoryCollection(new Dictionary<string, string?>
                        {
                            ["Jwt:SecretKey"] = TestJwtSecret
                        });
                    });

                    builder.ConfigureServices(services =>
                    {
                        services.RemoveAll<DbContextOptions<ApplicationDbContext>>();
                        services.RemoveAll<ApplicationDbContext>();
                        services.RemoveAll<DbContext>();
                        services.RemoveAll<IAiIntegrationService>();

                        services.AddDbContext<ApplicationDbContext>(options => options.UseSqlite(_connection));
                        services.AddScoped<DbContext>(provider => provider.GetRequiredService<ApplicationDbContext>());
                        services.AddScoped(_ => aiIntegrationService);

                        services.AddAuthentication(options =>
                        {
                            options.DefaultAuthenticateScheme = TestAuthHandler.SchemeName;
                            options.DefaultChallengeScheme = TestAuthHandler.SchemeName;
                        }).AddScheme<AuthenticationSchemeOptions, TestAuthHandler>(TestAuthHandler.SchemeName, _ => { });

                        services.PostConfigureAll<AuthenticationOptions>(options =>
                        {
                            options.DefaultAuthenticateScheme = TestAuthHandler.SchemeName;
                            options.DefaultChallengeScheme = TestAuthHandler.SchemeName;
                            options.DefaultScheme = TestAuthHandler.SchemeName;
                        });

                        services.AddAuthorization(options =>
                        {
                            var policy = new AuthorizationPolicyBuilder(TestAuthHandler.SchemeName)
                                .RequireAuthenticatedUser()
                                .Build();
                            options.DefaultPolicy = policy;
                            options.FallbackPolicy = policy;
                        });

                        using var scope = services.BuildServiceProvider().CreateScope();
                        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                        dbContext.Database.EnsureCreated();
                    });
                });
        }

        public async Task SeedAsync(params object[] entities)
        {
            using var scope = Factory.Services.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            dbContext.AddRange(entities);
            await dbContext.SaveChangesAsync();
        }

        public async Task<List<AiHealthInsight>> GetAiHealthInsightsAsync()
        {
            using var scope = Factory.Services.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            return await dbContext.AiHealthInsights
                .IgnoreQueryFilters()
                .OrderBy(x => x.GeneratedAt)
                .ToListAsync();
        }

        public void Dispose()
        {
            Factory.Dispose();
            _connection.Dispose();
        }
    }
}
