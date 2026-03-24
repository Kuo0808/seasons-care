using Microsoft.Extensions.Configuration;
using SeasonsCare.Api.DTOs.Auth;
using SeasonsCare.Api.Exceptions;
using SeasonsCare.Api.Models.Entities;
using SeasonsCare.Api.Repositories;
using SeasonsCare.Api.Services;

namespace SeasonsCare.Api.Tests.Services;

public class AuthServiceTests
{
    [Fact]
    public async Task RegisterAsync_ThrowsConflict_WhenEmailAlreadyExists()
    {
        var repository = new FakeUserRepository(emailExists: true);
        var service = new AuthService(repository, BuildConfiguration());

        var exception = await Assert.ThrowsAsync<DomainException>(() =>
            service.RegisterAsync(new RegisterRequest
            {
                Email = "tester@example.com",
                Password = "Password1",
                Username = "tester"
            }));

        Assert.Equal(409, exception.StatusCode);
        Assert.Equal("EMAIL_ALREADY_EXISTS", exception.ErrorCode);
    }

    [Fact]
    public async Task RegisterAsync_StoresLowercaseEmail_AndHashedPassword()
    {
        var repository = new FakeUserRepository();
        var service = new AuthService(repository, BuildConfiguration());

        await service.RegisterAsync(new RegisterRequest
        {
            Email = "Tester@Example.COM",
            Password = "Password1",
            Username = "tester"
        });

        var savedUser = Assert.Single(repository.Users);
        Assert.Equal("tester@example.com", savedUser.Email);
        Assert.Equal("tester", savedUser.Username);
        Assert.NotEqual("Password1", savedUser.PasswordHash);
        Assert.True(BCrypt.Net.BCrypt.Verify("Password1", savedUser.PasswordHash));
    }

    [Fact]
    public async Task LoginAsync_ThrowsUnauthorized_WhenPasswordIsWrong()
    {
        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = "tester@example.com",
            Username = "tester",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("Password1")
        };

        var repository = new FakeUserRepository(user: user);
        var service = new AuthService(repository, BuildConfiguration());

        var exception = await Assert.ThrowsAsync<DomainException>(() =>
            service.LoginAsync(new LoginRequest
            {
                Email = user.Email,
                Password = "WrongPassword1"
            }));

        Assert.Equal(401, exception.StatusCode);
        Assert.Equal("LOGIN_FAILED", exception.ErrorCode);
    }

    [Fact]
    public async Task LoginAsync_ReturnsJwtAndUserPayload_WhenCredentialsAreValid()
    {
        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = "tester@example.com",
            Username = "tester",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("Password1")
        };

        var repository = new FakeUserRepository(user: user);
        var service = new AuthService(repository, BuildConfiguration());

        var result = await service.LoginAsync(new LoginRequest
        {
            Email = "Tester@Example.com",
            Password = "Password1"
        });

        Assert.False(string.IsNullOrWhiteSpace(result.Token));
        Assert.Equal(user.Id, result.User.Id);
        Assert.Equal(user.Email, result.User.Email);
        Assert.Equal(user.Username, result.User.Username);
    }

    private static IConfiguration BuildConfiguration()
    {
        return new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Jwt:SecretKey"] = "this-is-a-test-secret-key-with-enough-length",
                ["Jwt:Issuer"] = "seasons-care-tests",
                ["Jwt:Audience"] = "seasons-care-tests"
            })
            .Build();
    }

    private sealed class FakeUserRepository : IUserRepository
    {
        private readonly bool _emailExists;
        private readonly User? _user;

        public List<User> Users { get; } = new();

        public FakeUserRepository(bool emailExists = false, User? user = null)
        {
            _emailExists = emailExists;
            _user = user;

            if (user is not null)
            {
                Users.Add(user);
            }
        }

        public Task<bool> EmailExistsAsync(string email)
        {
            return Task.FromResult(_emailExists || Users.Any(x => x.Email == email));
        }

        public Task<User?> GetByEmailAsync(string email)
        {
            return Task.FromResult(Users.FirstOrDefault(x => x.Email == email));
        }

        public Task AddAsync(User user)
        {
            Users.Add(user);
            return Task.CompletedTask;
        }

        public Task SaveChangesAsync()
        {
            return Task.CompletedTask;
        }
    }
}
