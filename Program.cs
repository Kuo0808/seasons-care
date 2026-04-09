using FluentValidation;
using FluentValidation.AspNetCore;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using SeasonsCare.Api.Config.DependencyInjection;
using SeasonsCare.Api.Middleware;
using SeasonsCare.Api.Repositories;
using SeasonsCare.Api.Services;
using SeasonsCare.Api.Validations.Auth;
using SeasonsCare.Api.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using System.IO;
using Microsoft.OpenApi.Models;
using SeasonsCare.Api.Config.Json;

// [架構導覽] 階段一：應用程式建構與設定初始化 (Application Bootstrapping)
// 載入 appsettings.json、環境變數，並準備註冊所需元件。
var builder = WebApplication.CreateBuilder(args);
var isSwaggerEnabled = builder.Environment.IsDevelopment() || builder.Configuration.GetValue<bool>("Swagger:Enabled");
const string JwtPlaceholderSecret = "<YOUR_JWT_SECRET_KEY_AT_LEAST_32_CHARS>";

// [架構導覽] 階段二：服務註冊 (Service Registration)
// 將 Controller、驗證規則等基礎服務註冊至相依性注入 (DI) 容器中。
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        // 讓前端可直接傳遞 repeatPattern 字串，例如 none、daily、weeklyDay、monthly。
        options.JsonSerializerOptions.Converters.Add(new EventRepeatPatternJsonConverter());
        options.JsonSerializerOptions.Converters.Add(new EventOccurrenceStatusJsonConverter());
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
                "資料驗證失敗",
                "VALIDATION_FAILED",
                400,
                errors
            );
        };
    });

// 了解更多關於 Swagger/OpenAPI 的設定：https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();

builder.Services.AddCors(options =>
{
    options.AddPolicy("FrontendCorsPolicy", policyBuilder =>
    {
        // 開發階段的暫時設定。在上線正式環境前，請務必嚴格限制為明確的來源網域 (Origins)。
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

    // 設定 Swagger 支援 JWT 授權
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "Bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "請輸入 'Bearer' 加上空白後，再輸入您的 Token。\n例如: `Bearer eyJhbGci...`"
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

// 註冊 FluentValidation 驗證工具
builder.Services.AddFluentValidationAutoValidation();                                          //註冊 FluentValidation
builder.Services.AddValidatorsFromAssemblyContaining<RegisterRequestValidator>();

// 註冊 DbContext 狀態與連線
builder.Services.AddDbContext<ApplicationDbContext>(options =>                                     //註冊 DbContext
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection"))
           .UseSnakeCaseNamingConvention());

// 設定全域型別對應：當系統需要 DbContext 介面時，統一提供 ApplicationDbContext 這個具體實作
builder.Services.AddScoped<DbContext>(provider => provider.GetRequiredService<ApplicationDbContext>());              //註冊 DbContext

// [架構導覽] 階段三：依賴注入 - 資料存取層 (Data Access Layer)
// 註冊 Repository。當系統請求 IRepository 時，DI 容器將提供其實作類別。
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<ICareGroupRepository, CareGroupRepository>();
builder.Services.AddScoped<ICareLogRepository, CareLogRepository>();
builder.Services.AddScoped<IEventSeriesRepository, EventSeriesRepository>();
builder.Services.AddScoped<IEventOccurrenceRepository, EventOccurrenceRepository>();
builder.Services.AddScoped<IExpenseRepository, ExpenseRepository>();
builder.Services.AddScoped<IAiHealthInsightRepository, AiHealthInsightRepository>();

// [架構導覽] 階段四：依賴注入 - 商業邏輯層 (Business Logic Layer)
// 註冊 Service 服務。系統將從此處解析 Controller 所需的商業邏輯與介面實作。
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICurrentUserService, CurrentUserService>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<ICareGroupService, CareGroupService>();
builder.Services.AddScoped<ICareLogService, CareLogService>();
builder.Services.AddScoped<IEventSeriesService, EventSeriesService>();
builder.Services.AddScoped<IEventOccurrenceService, EventOccurrenceService>();
builder.Services.AddScoped<IExpenseService, ExpenseService>();
builder.Services.AddScoped<IAiHealthInsightService, AiHealthInsightService>();
builder.Services.AddHealthRecordsModule();

if (!builder.Environment.IsEnvironment("Testing") &&
    string.Equals(builder.Configuration["Jwt:SecretKey"], JwtPlaceholderSecret, StringComparison.Ordinal))
{
    throw new InvalidOperationException("JWT SecretKey is still using the placeholder value. Configure a real secret before starting the API.");
}

// 設定身分驗證 (Authentication) 與 授權 (Authorization) 機制
var jwtSettings = builder.Configuration.GetSection("Jwt");                 //從 appsettings.json 中取得 JWT 設定
var secretKey = jwtSettings["SecretKey"]
    ?? throw new InvalidOperationException("JWT SecretKey 未設定，請確認 Environment Variables 或 appsettings。");

builder.Services.AddAuthentication(options =>                              //設定認證
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,                         //這 token 是不是我認可的人發的
        ValidateAudience = true,                       //這 token 是不是我認可的接收者
        ValidateLifetime = true,                       //這 token 是否在有效期限內
        ValidateIssuerSigningKey = true,                 //這 token 的簽名是否正確
        ValidIssuer = jwtSettings["Issuer"],             //這 token 的發行者是誰
        ValidAudience = jwtSettings["Audience"],         //這 token 的接收者是誰
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey))
    };
});

var app = builder.Build();

// [架構導覽] 階段五：HTTP 請求管線設定 (HTTP Request Pipeline)
// 決定進入伺服器的 Request 將依序通過哪些中介軟體 (Middleware) 的過濾與處理。

// 自動執行資料庫遷移 (取代 GitHub Action 中的 ef update)
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
app.MapControllers();

app.Run();

public partial class Program { }
