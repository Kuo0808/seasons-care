using System.Net;
using System.Net.Http.Json;
using SeasonsCare.Api.DTOs.Auth;
using SeasonsCare.Api.Exceptions;
using SeasonsCare.Api.Services;
using SeasonsCare.Api.Tests.Shared;
using SeasonsCare.Api.Tests.Shared.Http;

namespace SeasonsCare.Api.Tests.Integration.Auth;

public class AuthControllerIntegrationTests
{
    [Fact]
    public async Task Register_ReturnsBadRequest_WhenEmailIsInvalid()
    {
        using var factory = new StubApiFactory<IAuthService>(new StubAuthService());
        using var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/auth/register", new
        {
            email = "invalid-email",
            password = "Password1",
            username = "tester",
            avatarKey = "dog_01"
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        using var payload = await JsonResponseHelper.ReadJsonAsync(response);
        Assert.Equal("VALIDATION_FAILED", payload.RootElement.GetProperty("errorCode").GetString());
    }

    [Fact]
    public async Task Login_ReturnsBadRequest_WhenPasswordIsMissing()
    {
        using var factory = new StubApiFactory<IAuthService>(new StubAuthService());
        using var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/auth/login", new
        {
            email = "tester@example.com",
            password = ""
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        using var payload = await JsonResponseHelper.ReadJsonAsync(response);
        Assert.Equal("INVALID_LOGIN_REQUEST", payload.RootElement.GetProperty("errorCode").GetString());
    }

    [Fact]
    public async Task Login_ReturnsUnauthorized_WhenServiceRejectsCredentials()
    {
        using var factory = new StubApiFactory<IAuthService>(new StubAuthService
        {
            LoginException = new DomainException("unauthorized", "LOGIN_FAILED", 401)
        });
        using var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/auth/login", new
        {
            email = "tester@example.com",
            password = "WrongPassword1"
        });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);

        using var payload = await JsonResponseHelper.ReadJsonAsync(response);
        Assert.Equal("LOGIN_FAILED", payload.RootElement.GetProperty("errorCode").GetString());
    }

    [Fact]
    public async Task Login_ReturnsOk_WhenServiceReturnsToken()
    {
        using var factory = new StubApiFactory<IAuthService>(new StubAuthService());
        using var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/auth/login", new
        {
            email = "tester@example.com",
            password = "Password1"
        });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        using var payload = await JsonResponseHelper.ReadJsonAsync(response);
        Assert.True(payload.RootElement.GetProperty("success").GetBoolean());
        Assert.Equal("stub-token", payload.RootElement.GetProperty("data").GetProperty("token").GetString());
    }

    private sealed class StubAuthService : IAuthService
    {
        public Exception? LoginException { get; init; }

        public Task RegisterAsync(RegisterRequest request)
        {
            return Task.CompletedTask;
        }

        public Task<LoginResponse> LoginAsync(LoginRequest request)
        {
            if (LoginException is not null)
            {
                throw LoginException;
            }

            return Task.FromResult(new LoginResponse
            {
                Token = "stub-token",
                User = new UserDto
                {
                    Id = Guid.Parse("11111111-1111-1111-1111-111111111111"),
                    Email = request.Email,
                    Username = "tester",
                    AvatarKey = "dog_01"
                }
            });
        }
    }
}
