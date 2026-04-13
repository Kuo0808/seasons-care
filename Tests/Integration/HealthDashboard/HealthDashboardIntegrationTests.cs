using System.Net;
using System.Text.Json;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
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
    public async Task GetDashboard_ReturnsCachedInsight_WithoutCallingAiIntegration()
    {
        var fakeAiService = new FakeAiIntegrationService();
        using var factory = new HealthDashboardApiFactory(fakeAiService);
        using var client = factory.Factory.CreateClient();

        var careGroup = SeedDataHelper.CreateCareGroup("Group A");
        var (dateFrom, dateTo) = GetDashboardRange();
        var cachedInsight = SeedDataHelper.CreateAiHealthInsight(careGroup.Id, "health_dashboard_7d", DateTime.UtcNow);
        cachedInsight.DateFrom = dateFrom;
        cachedInsight.DateTo = dateTo;
        cachedInsight.OverallSummary = "cached summary";
        cachedInsight.TodaySummary = "cached today summary";
        cachedInsight.KeyInsights = "cached insights";
        cachedInsight.Recommendations = "cached recommendations";
        cachedInsight.TrendLabels = "{\"bloodPressure\":\"趨勢良好\",\"bloodOxygen\":\"穩定\",\"bloodSugar\":\"建議觀察\",\"temperature\":\"穩定\",\"weight\":\"逐步改善\"}";

        await factory.SeedAsync(
            SeedDataHelper.CreateUser(),
            careGroup,
            SeedDataHelper.CreateMember(careGroup.Id),
            SeedDataHelper.CreateBloodPressure(careGroup.Id, 128, 84, dateFrom.AddDays(6).AddHours(2)),
            SeedDataHelper.CreateBloodSugar(careGroup.Id, 112m, "飯前", dateFrom.AddDays(6).AddHours(3)),
            SeedDataHelper.CreateWeight(careGroup.Id, 61.5m, dateFrom.AddDays(5)),
            SeedDataHelper.CreateTemperature(careGroup.Id, 36.7m, dateFrom.AddDays(6).AddHours(1)),
            SeedDataHelper.CreateBloodOxygen(careGroup.Id, 98m, dateFrom.AddDays(6).AddHours(4)),
            cachedInsight);

        var response = await client.GetAsync($"/api/care-groups/{careGroup.Id}/health-dashboard");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        using var payload = await JsonResponseHelper.ReadJsonAsync(response);
        var data = payload.RootElement.GetProperty("data");

        Assert.True(data.GetProperty("isFromCache").GetBoolean());
        Assert.Equal("cached summary", data.GetProperty("aiReport").GetProperty("overallSummary").GetString());
        Assert.Equal("cached today summary", data.GetProperty("todaySummary").GetProperty("summaryText").GetString());
        Assert.Equal("趨勢良好", data.GetProperty("trendLabels").GetProperty("bloodPressure").GetString());
        Assert.Equal(0, fakeAiService.CallCount);
    }

    [Fact]
    public async Task GetDashboard_GeneratesAndPersistsInsight_WhenCacheMiss()
    {
        var fakeAiService = new FakeAiIntegrationService
        {
            Result = new AiGeneratedInsightDto
            {
                OverallSummary = "generated summary",
                TodaySummary = "generated today summary",
                KeyInsights = "generated insights",
                Recommendations = "generated recommendations",
                TrendLabels = new TrendLabelsDto
                {
                    BloodPressure = "趨勢良好",
                    BloodOxygen = "穩定",
                    BloodSugar = "建議觀察",
                    Temperature = "穩定",
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

        var response = await client.GetAsync($"/api/care-groups/{careGroup.Id}/health-dashboard");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        using var payload = await JsonResponseHelper.ReadJsonAsync(response);
        var data = payload.RootElement.GetProperty("data");

        Assert.False(data.GetProperty("isFromCache").GetBoolean());
        Assert.Equal("generated summary", data.GetProperty("aiReport").GetProperty("overallSummary").GetString());
        Assert.Equal("generated today summary", data.GetProperty("todaySummary").GetProperty("summaryText").GetString());
        Assert.Equal("趨勢良好", data.GetProperty("trendLabels").GetProperty("bloodPressure").GetString());
        Assert.Equal(1, fakeAiService.CallCount);

        var insights = await factory.GetAiHealthInsightsAsync();
        Assert.Single(insights);
        Assert.Equal("health_dashboard_7d", insights[0].ReportType);
        Assert.Equal("generated summary", insights[0].OverallSummary);
        Assert.Contains("bloodPressure", insights[0].TrendLabels);
    }

    private static (DateTime DateFrom, DateTime DateTo) GetDashboardRange()
    {
        var todayStart = NormalizeTimestamp(DateTime.UtcNow.Date);
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
            OverallSummary = "default summary",
            TodaySummary = "default today summary",
            KeyInsights = "default insights",
            Recommendations = "default recommendations",
            TrendLabels = new TrendLabelsDto
            {
                BloodPressure = "穩定",
                BloodOxygen = "穩定",
                BloodSugar = "穩定",
                Temperature = "穩定",
                Weight = "穩定"
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
