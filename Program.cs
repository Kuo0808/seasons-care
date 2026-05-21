using System.IdentityModel.Tokens.Jwt;
using System.IO;
using System.Security.Claims;
using System.Text;
using FluentValidation;
using FluentValidation.AspNetCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using SeasonsCare.Api.Config.DependencyInjection;
using SeasonsCare.Api.Config.Json;
using SeasonsCare.Api.Config.OpenAI;
using SeasonsCare.Api.Data;
using SeasonsCare.Api.Hubs;
using SeasonsCare.Api.Middleware;
using SeasonsCare.Api.Repositories;
using SeasonsCare.Api.Services;
using SeasonsCare.Api.Services.AI;
using SeasonsCare.Api.Services.HealthDashboard;
using SeasonsCare.Api.Validations.Auth;

var builder = WebApplication.CreateBuilder(args);
var isSwaggerEnabled = builder.Environment.IsDevelopment() || builder.Configuration.GetValue<bool>("Swagger:Enabled");
const string JwtPlaceholderSecret = "<YOUR_JWT_SECRET_KEY_AT_LEAST_32_CHARS>";

builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.Converters.Add(new EventRepeatPatternJsonConverter());
        options.JsonSerializerOptions.Converters.Add(new EventOccurrenceStatusJsonConverter());
        options.JsonSerializerOptions.Converters.Add(new ExpenseSplitStatusJsonConverter());
    })
    .ConfigureApiBehaviorOptions(options =>
    {
        options.InvalidModelStateResponseFactory = context =>
        {
            var errors = context.ModelState
                .Where(x => x.Value != null && x.Value.Errors.Count > 0)
                .ToDictionary(
                    kvp => string.IsNullOrEmpty(kvp.Key) ? "body" : char.ToLowerInvariant(kvp.Key[0]) + kvp.Key.Substring(1),
                    kvp => kvp.Value!.Errors.Select(e => e.ErrorMessage).ToArray()
                );

            throw new SeasonsCare.Api.Exceptions.DomainException(
                "Validation failed.",
                "VALIDATION_FAILED",
                400,
                errors
            );
        };
    });

builder.Services.AddEndpointsApiExplorer();

builder.Services.AddCors(options =>
{
    options.AddPolicy("FrontendCorsPolicy", policyBuilder =>
    {
        policyBuilder.AllowAnyOrigin()
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});

builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "Seasons Care API", Version = "v1" });
    var xmlFile = $"{System.Reflection.Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    if (File.Exists(xmlPath))
    {
        c.IncludeXmlComments(xmlPath, includeControllerXmlComments: true);
    }

    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "Bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Use the Bearer scheme. Example: `Bearer eyJhbGci...`"
    });

    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});

builder.Services.AddFluentValidationAutoValidation();
builder.Services.AddValidatorsFromAssemblyContaining<RegisterRequestValidator>();

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection"))
        .UseSnakeCaseNamingConvention());
builder.Services.AddScoped<DbContext>(provider => provider.GetRequiredService<ApplicationDbContext>());

builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<ICareGroupRepository, CareGroupRepository>();
builder.Services.AddScoped<ICareLogRepository, CareLogRepository>();
builder.Services.AddScoped<IEventSeriesRepository, EventSeriesRepository>();
builder.Services.AddScoped<IEventOccurrenceRepository, EventOccurrenceRepository>();
builder.Services.AddScoped<IExpenseRepository, ExpenseRepository>();
builder.Services.AddScoped<IExpenseSplitRepository, ExpenseSplitRepository>();
builder.Services.AddScoped<IAiHealthInsightRepository, AiHealthInsightRepository>();
builder.Services.AddScoped<INotificationRepository, NotificationRepository>();

builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICurrentUserService, CurrentUserService>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<ICareGroupService, CareGroupService>();
builder.Services.AddScoped<ICareLogService, CareLogService>();
builder.Services.AddScoped<IEventSeriesService, EventSeriesService>();
builder.Services.AddScoped<IEventOccurrenceService, EventOccurrenceService>();
builder.Services.AddScoped<IEventFacadeService, EventFacadeService>();
builder.Services.AddScoped<IExpenseService, ExpenseService>();
builder.Services.AddScoped<IAiHealthInsightService, AiHealthInsightService>();
builder.Services.AddScoped<INotificationService, NotificationService>();
builder.Services.Configure<OpenAiOptions>(builder.Configuration.GetSection("OpenAI"));
builder.Services.AddHttpClient<IAiIntegrationService, OpenAiIntegrationService>();
builder.Services.AddScoped<IHealthDashboardService, HealthDashboardService>();
builder.Services.AddHealthRecordsModule();
builder.Services.AddSignalR();

if (!builder.Environment.IsEnvironment("Testing") &&
    string.Equals(builder.Configuration["Jwt:SecretKey"], JwtPlaceholderSecret, StringComparison.Ordinal))
{
    throw new InvalidOperationException("JWT SecretKey is still using the placeholder value. Configure a real secret before starting the API.");
}

var jwtSettings = builder.Configuration.GetSection("Jwt");
var secretKey = jwtSettings["SecretKey"]
    ?? throw new InvalidOperationException("JWT SecretKey is not configured.");

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = jwtSettings["Issuer"],
        ValidAudience = jwtSettings["Audience"],
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey)),
        NameClaimType = JwtRegisteredClaimNames.Sub,
        RoleClaimType = ClaimTypes.Role
    };

    options.Events = new JwtBearerEvents
    {
        OnMessageReceived = context =>
        {
            var accessToken = context.Request.Query["access_token"];
            var path = context.HttpContext.Request.Path;
            if (!string.IsNullOrWhiteSpace(accessToken) && path.StartsWithSegments(NotificationHub.HubRoute))
            {
                context.Token = accessToken;
            }

            return Task.CompletedTask;
        }
    };
});

var app = builder.Build();

if (!app.Environment.IsEnvironment("Testing"))
{
    using var scope = app.Services.CreateScope();
    var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    dbContext.Database.Migrate();
}

app.UseForwardedHeaders(new ForwardedHeadersOptions
{
    ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto
});

if (isSwaggerEnabled)
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseMiddleware<GlobalExceptionMiddleware>();

app.UseHttpsRedirection();

app.UseRouting();
app.UseCors("FrontendCorsPolicy");

app.UseMiddleware<CareGroupContextMiddleware>();

app.UseAuthentication();
app.UseAuthorization();

app.MapGet("/healthz", async (ApplicationDbContext dbContext) =>
{
    var canConnect = await dbContext.Database.CanConnectAsync();
    return canConnect
        ? Results.Ok(new { status = "ok" })
        : Results.Problem(statusCode: StatusCodes.Status503ServiceUnavailable, title: "Database unavailable.");
}).AllowAnonymous();

app.MapControllers();
app.MapHub<NotificationHub>(NotificationHub.HubRoute);

app.Run();

public partial class Program { }
