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
        cachedInsight.OverallSummary = "過去七天趨勢穩定";
        cachedInsight.TodaySummary = "今天血壓已完成量測，建議晚餐少鹽。";
        cachedInsight.KeyInsights = "血糖在週三與週五偏高";
        cachedInsight.Recommendations = "建議控制精緻澱粉並維持飯後步行。";

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
        Assert.Equal("過去七天趨勢穩定", data.GetProperty("overallSummary").GetString());
        Assert.Equal("血糖在週三與週五偏高", data.GetProperty("keyInsight").GetString());
        Assert.Equal("建議控制精緻澱粉並維持飯後步行。", data.GetProperty("actionSuggestion").GetString());
        Assert.Equal(0, fakeAiService.CallCount);
    }

    [Fact]
    public async Task GetWeeklyInsight_GeneratesAndPersistsInsight_WhenCacheMiss()
    {
        var fakeAiService = new FakeAiIntegrationService
        {
            Result = new AiGeneratedInsightDto
            {
                OverallSummary = "七天趨勢大致穩定",
                TodaySummary = "下午已完成血壓量測，建議晚餐清淡。",
                KeyInsights = "飯後血糖略有波動",
                Recommendations = "建議維持低糖飲食並增加飯後步行。",
                TrendLabels = new TrendLabelsDto
                {
                    BloodPressure = "穩定",
                    BloodOxygen = "正常",
                    BloodSugar = "需要觀察",
                    Temperature = "正常",
                    Weight = "略為上升"
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
        Assert.Equal("七天趨勢大致穩定", data.GetProperty("overallSummary").GetString());
        Assert.Equal("飯後血糖略有波動", data.GetProperty("keyInsight").GetString());
        Assert.Equal("建議維持低糖飲食並增加飯後步行。", data.GetProperty("actionSuggestion").GetString());
        Assert.Equal(1, fakeAiService.CallCount);

        var insights = await factory.GetAiHealthInsightsAsync();
        Assert.Single(insights);
        Assert.Equal("health_dashboard_7d", insights[0].ReportType);
        Assert.Equal("七天趨勢大致穩定", insights[0].OverallSummary);
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
        cachedInsight.OverallSummary = "過去七天健康狀態大致穩定但血糖在飯後仍有小幅度波動需要持續追蹤";
        cachedInsight.TodaySummary = "今日已完成量測。";
        cachedInsight.KeyInsights = "血糖波動主要集中在週三與週五的晚餐後時段";
        cachedInsight.Recommendations = "建議控制精緻澱粉攝取，並維持飯後 15 分鐘散步。";

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
        Assert.Equal("建議控制精緻澱粉攝取，並維持飯後 15 分鐘散步。", data.GetProperty("actionSuggestion").GetString());
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
                OverallSummary = "七天趨勢大致穩定",
                TodaySummary = "今天血壓偏高，建議晚餐減少鹽分攝取。",
                KeyInsights = "今日血壓需要觀察",
                Recommendations = "減少鹽分並提早休息。",
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
        Assert.Equal("今天血壓偏高，建議晚餐減少鹽分攝取。", data.GetProperty("summary").GetString());
        Assert.Equal(1, data.GetProperty("recordCount").GetInt32());
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
            OverallSummary = "七天趨勢穩定",
            TodaySummary = "今日已完成量測，建議持續觀察。",
            KeyInsights = "血糖略有波動",
            Recommendations = "建議維持規律飲食與作息。",
            TrendLabels = new TrendLabelsDto
            {
                BloodPressure = "正常",
                BloodOxygen = "正常",
                BloodSugar = "正常",
                Temperature = "正常",
                Weight = "正常"
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
