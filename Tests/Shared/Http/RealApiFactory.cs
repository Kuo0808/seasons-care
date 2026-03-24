using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using SeasonsCare.Api.Data;
using SeasonsCare.Api.Models.Entities;
using SeasonsCare.Api.Models.Entities.HealthRecords;

namespace SeasonsCare.Api.Tests.Shared.Http;

public class RealApiFactory : IDisposable
{
    private readonly SqliteConnection _connection;
    public WebApplicationFactory<Program> Factory { get; }

    public RealApiFactory()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();

        Factory = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder =>
            {
                builder.ConfigureServices(services =>
                {
                    services.RemoveAll<DbContextOptions<ApplicationDbContext>>();
                    services.RemoveAll<ApplicationDbContext>();
                    services.RemoveAll<DbContext>();

                    services.AddDbContext<ApplicationDbContext>(options =>
                        options.UseSqlite(_connection));
                    services.AddScoped<DbContext>(provider => provider.GetRequiredService<ApplicationDbContext>());

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

    public async Task<T?> FindAsync<T>(Guid id) where T : class
    {
        using var scope = Factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        if (typeof(T) == typeof(CareLog))
        {
            var entity = await dbContext.CareLogs.IgnoreQueryFilters().FirstOrDefaultAsync(x => x.Id == id);
            return entity as T;
        }

        if (typeof(T) == typeof(ExpenseRecord))
        {
            var entity = await dbContext.Expenses.IgnoreQueryFilters().FirstOrDefaultAsync(x => x.Id == id);
            return entity as T;
        }

        if (typeof(T) == typeof(BloodSugarRecord))
        {
            var entity = await dbContext.BloodSugars.IgnoreQueryFilters().FirstOrDefaultAsync(x => x.Id == id);
            return entity as T;
        }

        if (typeof(T) == typeof(BloodPressureRecord))
        {
            var entity = await dbContext.BloodPressures.IgnoreQueryFilters().FirstOrDefaultAsync(x => x.Id == id);
            return entity as T;
        }

        return await dbContext.Set<T>().FindAsync(id);
    }

    public async Task<List<CareLog>> GetCareLogsAsync()
    {
        using var scope = Factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        return await dbContext.CareLogs.IgnoreQueryFilters().OrderBy(x => x.Title).ToListAsync();
    }

    public async Task<List<ExpenseRecord>> GetExpensesAsync()
    {
        using var scope = Factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        return await dbContext.Expenses.IgnoreQueryFilters().OrderBy(x => x.Title).ToListAsync();
    }

    public async Task<List<BloodSugarRecord>> GetBloodSugarsAsync()
    {
        using var scope = Factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        return await dbContext.BloodSugars.IgnoreQueryFilters().OrderBy(x => x.RecordDate).ToListAsync();
    }

    public async Task<List<BloodPressureRecord>> GetBloodPressuresAsync()
    {
        using var scope = Factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        return await dbContext.BloodPressures.IgnoreQueryFilters().OrderBy(x => x.RecordDate).ToListAsync();
    }

    public void Dispose()
    {
        Factory.Dispose();
        _connection.Dispose();
    }
}
