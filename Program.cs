using FluentValidation;
using FluentValidation.AspNetCore;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using SeasonsCare.Api.Middleware;
using SeasonsCare.Api.Repositories;
using SeasonsCare.Api.Services;
using SeasonsCare.Api.Validations.Auth;
using SeasonsCare.Api.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers()
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

// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();

var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? Array.Empty<string>();
builder.Services.AddCors(options =>
{
    options.AddPolicy("FrontendCorsPolicy", policyBuilder =>
    {
        policyBuilder.WithOrigins(allowedOrigins)
                     .AllowAnyHeader()
                     .AllowAnyMethod();
    });
});

builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "Seasons Care API", Version = "v1" });

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

// Register FluentValidation
builder.Services.AddFluentValidationAutoValidation();                                          //註冊 FluentValidation
builder.Services.AddValidatorsFromAssemblyContaining<RegisterRequestValidator>();

// Register Repositories and Services
builder.Services.AddDbContext<ApplicationDbContext>(options =>                                     //註冊 DbContext
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection"))
           .UseSnakeCaseNamingConvention());

// Override DbContext registration for IUserRepository if needed, or we can register ApplicationDbContext as DbContext
builder.Services.AddScoped<DbContext>(provider => provider.GetRequiredService<ApplicationDbContext>());              //註冊 DbContext

// Repositories
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<ICareGroupRepository, CareGroupRepository>();
builder.Services.AddScoped<ICareLogRepository, CareLogRepository>();
builder.Services.AddScoped<IExpenseRepository, ExpenseRepository>();
builder.Services.AddScoped<IBloodPressureRepository, BloodPressureRepository>();

// Services
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICurrentUserService, CurrentUserService>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<ICareGroupService, CareGroupService>();
builder.Services.AddScoped<ICareLogService, CareLogService>();
builder.Services.AddScoped<IExpenseService, ExpenseService>();
builder.Services.AddScoped<IBloodPressureService, BloodPressureService>();

// Configure Authentication & Authorization
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

app.UseSwagger();
app.UseSwaggerUI();

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
