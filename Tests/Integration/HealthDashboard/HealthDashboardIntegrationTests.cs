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
    [Fact]
    public async Task GetWeeklyInsight_ReturnsCachedInsight_WithoutCallingAiIntegration()
    {
        var fakeAiService = new FakeAiIntegrationService();
        using var factory = new HealthDashboardApiFactory(fakeAiService);
        using var client = factory.Factory.CreateClient();

        var careGroup = SeedDataHelper.CreateCareGroup("Group A");
        var (dateFrom, dateTo) = GetDashboardRange();
        var cachedInsight = SeedDataHelper.CreateAiHealthInsight(careGroup.Id, "health_dashboard_7d", DateTime.UtcNow);
        cachedInsight.DateFrom = dateFrom;
        cachedInsight.DateTo = dateTo;
        cachedInsight.OverallSummary = "近七天狀況大致穩定";
        cachedInsight.TodaySummary = "今天量測完成，建議晚上持續補水。";
        cachedInsight.KeyInsights = "血糖起伏集中在晚餐後";
        cachedInsight.Recommendations = "飲食先減少精緻澱粉，並固定散步時間。";

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
        Assert.Equal("近七天狀況大致穩定", data.GetProperty("overallSummary").GetString());
        Assert.Equal("血糖起伏集中在晚餐後", data.GetProperty("keyInsight").GetString());
        Assert.Equal("飲食先減少精緻澱粉，並固定散步時間。", data.GetProperty("actionSuggestion").GetString());
        Assert.Equal(0, fakeAiService.CallCount);
    }

    [Fact]
    public async Task GetWeeklyInsight_GeneratesAndPersistsInsight_WhenCacheMiss()
    {
        var fakeAiService = new FakeAiIntegrationService
        {
            Result = new AiGeneratedInsightDto
            {
                OverallSummary = "近七天血壓與血糖趨穩",
                TodaySummary = "今天血壓偏高，建議晚餐清淡並提早休息。",
                KeyInsights = "血糖波動集中在晚餐後",
                Recommendations = "晚餐澱粉減量，飯後固定散步 15 分鐘。",
                TrendLabels = new TrendLabelsDto
                {
                    BloodPressure = "趨於穩定",
                    BloodOxygen = "維持良好",
                    BloodSugar = "建議觀察",
                    Temperature = "維持良好",
                    Weight = "逐步改善"
                },
                SourceDataHash = "generated-hash",
                ModelName = "gpt-test",
                PromptVersion = "test-v1",
                GeneratedAt = DateTime.UtcNow
            }
        };

        using var factory = new HealthDashboardApiFactory(fakeAiService);
        using var client = factory.Factory.CreateClient();

        var careGroup = SeedDataHelper.CreateCareGroup("Group A");
        var (dateFrom, _) = GetDashboardRange();

        await factory.SeedAsync(
            SeedDataHelper.CreateUser(),
            careGroup,
            SeedDataHelper.CreateMember(careGroup.Id),
            SeedDataHelper.CreateBloodPressure(careGroup.Id, 130, 86, dateFrom.AddDays(6).AddHours(2)),
            SeedDataHelper.CreateBloodSugar(careGroup.Id, 118m, "飯後", dateFrom.AddDays(6).AddHours(3)),
            SeedDataHelper.CreateWeight(careGroup.Id, 62m, dateFrom.AddDays(4)),
            SeedDataHelper.CreateTemperature(careGroup.Id, 36.8m, dateFrom.AddDays(6).AddHours(1)),
            SeedDataHelper.CreateBloodOxygen(careGroup.Id, 97m, dateFrom.AddDays(6).AddHours(4)));

        var response = await client.GetAsync($"/api/care-groups/{careGroup.Id}/health-dashboard/weekly-insight");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        using var payload = await JsonResponseHelper.ReadJsonAsync(response);
        var data = payload.RootElement.GetProperty("data");

        Assert.False(data.GetProperty("isFromCache").GetBoolean());
        Assert.Equal("近七天血壓與血糖趨穩", data.GetProperty("overallSummary").GetString());
        Assert.Equal("血糖波動集中在晚餐後", data.GetProperty("keyInsight").GetString());
        Assert.Equal("晚餐澱粉減量，飯後固定散步 15 分鐘。", data.GetProperty("actionSuggestion").GetString());
        Assert.Equal(1, fakeAiService.CallCount);

        var insights = await factory.GetAiHealthInsightsAsync();
        Assert.Single(insights);
        Assert.Equal("health_dashboard_7d", insights[0].ReportType);
        Assert.Equal("近七天血壓與血糖趨穩", insights[0].OverallSummary);
    }

    [Fact]
    public async Task GetWeeklyInsight_ReturnsShortenedFields()
    {
        var fakeAiService = new FakeAiIntegrationService();
        using var factory = new HealthDashboardApiFactory(fakeAiService);
        using var client = factory.Factory.CreateClient();

        var careGroup = SeedDataHelper.CreateCareGroup("Group A");
        var (dateFrom, dateTo) = GetDashboardRange();
        var cachedInsight = SeedDataHelper.CreateAiHealthInsight(careGroup.Id, "health_dashboard_7d", DateTime.UtcNow);
        cachedInsight.DateFrom = dateFrom;
        cachedInsight.DateTo = dateTo;
        cachedInsight.OverallSummary = "這是一段超過四十個字的每週分析摘要，用來驗證 API 會自動裁切長度避免前端卡片爆版。";
        cachedInsight.TodaySummary = "今天有兩筆量測。";
        cachedInsight.KeyInsights = "這是一段超過三十個字的關鍵洞察內容，用來驗證欄位回傳時有正確縮短。";
        cachedInsight.Recommendations = "建議晚餐減少澱粉並在飯後散步十五分鐘。";

        await factory.SeedAsync(
            SeedDataHelper.CreateUser(),
            careGroup,
            SeedDataHelper.CreateMember(careGroup.Id),
            cachedInsight);

        var response = await client.GetAsync($"/api/care-groups/{careGroup.Id}/health-dashboard/weekly-insight");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        using var payload = await JsonResponseHelper.ReadJsonAsync(response);
        var data = payload.RootElement.GetProperty("data");

        Assert.True(data.GetProperty("overallSummary").GetString()!.Length <= 40);
        Assert.True(data.GetProperty("keyInsight").GetString()!.Length <= 30);
        Assert.Equal("建議晚餐減少澱粉並在飯後散步十五分鐘。", data.GetProperty("actionSuggestion").GetString());
    }

    [Fact]
    public async Task GetTodayInsight_ReturnsEmptyMessage_WhenNoTodayRecords()
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
        Assert.Equal("當日尚未有紀錄，快來新增吧！", data.GetProperty("summary").GetString());
        Assert.Equal(0, data.GetProperty("recordCount").GetInt32());
    }

    [Fact]
    public async Task GetTodayInsight_UsesAiTodaySummary_WhenTodayHasRecords()
    {
        var fakeAiService = new FakeAiIntegrationService
        {
            Result = new AiGeneratedInsightDto
            {
                OverallSummary = "近七天血壓與血糖趨穩",
                TodaySummary = "今天血壓偏高，晚餐清淡並少喝咖啡。",
                KeyInsights = "今天血壓略高",
                Recommendations = "晚間減少咖啡因，提早休息。",
                TrendLabels = new TrendLabelsDto(),
                GeneratedAt = DateTime.UtcNow
            }
        };

        using var factory = new HealthDashboardApiFactory(fakeAiService);
        using var client = factory.Factory.CreateClient();

        var careGroup = SeedDataHelper.CreateCareGroup("Group A");
        var (dateFrom, _) = GetDashboardRange();

        await factory.SeedAsync(
            SeedDataHelper.CreateUser(),
            careGroup,
            SeedDataHelper.CreateMember(careGroup.Id),
            SeedDataHelper.CreateBloodPressure(careGroup.Id, 136, 88, dateFrom.AddDays(6).AddHours(2)));

        var response = await client.GetAsync($"/api/care-groups/{careGroup.Id}/health-dashboard/today-insight");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        using var payload = await JsonResponseHelper.ReadJsonAsync(response);
        var data = payload.RootElement.GetProperty("data");

        Assert.True(data.GetProperty("hasTodayRecords").GetBoolean());
        Assert.Equal("今天血壓偏高，晚餐清淡並少喝咖啡。", data.GetProperty("summary").GetString());
        Assert.Equal(1, data.GetProperty("recordCount").GetInt32());
    }

    [Fact]
    public async Task GetTrendOverview_ReturnsMetricCardsAndChartPoints()
    {
        var fakeAiService = new FakeAiIntegrationService
        {
            Result = new AiGeneratedInsightDto
            {
                OverallSummary = "近七天狀況穩定",
                TodaySummary = "今天量測完成。",
                KeyInsights = "趨勢平穩",
                Recommendations = "維持目前作息。",
                TrendLabels = new TrendLabelsDto
                {
                    BloodPressure = "維持良好",
                    BloodOxygen = "逐步改善",
                    BloodSugar = "建議觀察",
                    Temperature = "趨於穩定",
                    Weight = "維持良好"
                },
                GeneratedAt = DateTime.UtcNow
            }
        };

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
            SeedDataHelper.CreateBloodOxygen(careGroup.Id, 96m, dateFrom.AddHours(3)),
            SeedDataHelper.CreateBloodOxygen(careGroup.Id, 98m, dateFrom.AddDays(6).AddHours(3)),
            SeedDataHelper.CreateBloodSugar(careGroup.Id, 150m, "飯後", dateFrom.AddHours(4)),
            SeedDataHelper.CreateBloodSugar(careGroup.Id, 130m, "飯前", dateFrom.AddDays(6).AddHours(4)),
            SeedDataHelper.CreateTemperature(careGroup.Id, 36.4m, dateFrom.AddDays(1).AddHours(4)),
            SeedDataHelper.CreateTemperature(careGroup.Id, 36.8m, dateFrom.AddDays(6).AddHours(5)),
            SeedDataHelper.CreateWeight(careGroup.Id, 70.0m, dateFrom.AddDays(2).AddHours(4)),
            SeedDataHelper.CreateWeight(careGroup.Id, 70.2m, dateFrom.AddDays(6).AddHours(6)));

        var response = await client.GetAsync($"/api/care-groups/{careGroup.Id}/health-dashboard/trend-overview");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        using var payload = await JsonResponseHelper.ReadJsonAsync(response);
        var metrics = payload.RootElement.GetProperty("data").GetProperty("metrics");

        Assert.Equal(5, metrics.GetArrayLength());

        var bloodPressure = metrics.EnumerateArray().First(x => x.GetProperty("metricType").GetString() == "blood_pressure");
        Assert.Equal("維持良好", bloodPressure.GetProperty("statusLabel").GetString());
        Assert.Equal("130 / 84", bloodPressure.GetProperty("displayValue").GetString());
        Assert.Equal(7, bloodPressure.GetProperty("points").GetArrayLength());
        Assert.Equal(7, bloodPressure.GetProperty("secondaryPoints").GetArrayLength());

        var bloodSugar = metrics.EnumerateArray().First(x => x.GetProperty("metricType").GetString() == "blood_sugar");
        Assert.Equal("建議觀察", bloodSugar.GetProperty("statusLabel").GetString());
        Assert.Equal("140", bloodSugar.GetProperty("displayValue").GetString());

        var weight = metrics.EnumerateArray().First(x => x.GetProperty("metricType").GetString() == "weight");
        Assert.Equal("70.1", weight.GetProperty("displayValue").GetString());
    }

    private static (DateTime DateFrom, DateTime DateTo) GetDashboardRange()
    {
        var todayStart = NormalizeTimestamp(TimeHelper.GetTaiwanDateStartUtc());
        return (
            NormalizeTimestamp(todayStart.AddDays(-6)),
            NormalizeTimestamp(todayStart.AddDays(1).AddMilliseconds(-1))
        );
    }

    private static DateTime NormalizeTimestamp(DateTime value)
    {
        var utcValue = value.Kind == DateTimeKind.Utc ? value : value.ToUniversalTime();
        return new DateTime(utcValue.Ticks - (utcValue.Ticks % TimeSpan.TicksPerMillisecond), DateTimeKind.Utc);
    }

    private sealed class FakeAiIntegrationService : IAiIntegrationService
    {
        public int CallCount { get; private set; }

        public AiGeneratedInsightDto Result { get; set; } = new()
        {
            OverallSummary = "近七天健康趨勢穩定",
            TodaySummary = "今天已完成量測，建議維持固定作息。",
            KeyInsights = "血糖波動略高",
            Recommendations = "建議控制晚餐份量並固定飯後散步。",
            TrendLabels = new TrendLabelsDto
            {
                BloodPressure = "維持良好",
                BloodOxygen = "維持良好",
                BloodSugar = "維持良好",
                Temperature = "維持良好",
                Weight = "維持良好"
            },
            SourceDataHash = "default-hash",
            ModelName = "gpt-test",
            PromptVersion = "test-v1",
            GeneratedAt = DateTime.UtcNow
        };

        public Task<AiGeneratedInsightDto> GenerateHealthInsightAsync(HealthInsightPromptInput input)
        {
            CallCount++;
            return Task.FromResult(Result);
        }
    }

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

                        services.AddDbContext<ApplicationDbContext>(options =>
                            options.UseSqlite(_connection));
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
            return await dbContext.AiHealthInsights.IgnoreQueryFilters().OrderBy(x => x.GeneratedAt).ToListAsync();
        }

        public void Dispose()
        {
            Factory.Dispose();
            _connection.Dispose();
        }
    }
}
