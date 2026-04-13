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
        var service = new AuthService(repository, new FakeCareGroupRepository(), BuildConfiguration());

        var exception = await Assert.ThrowsAsync<DomainException>(() =>
            service.RegisterAsync(new RegisterRequest
            {
                Email = "tester@example.com",
                Password = "Password1"
            }));

        Assert.Equal(409, exception.StatusCode);
        Assert.Equal("EMAIL_ALREADY_EXISTS", exception.ErrorCode);
    }

    [Fact]
    public async Task RegisterAsync_StoresLowercaseEmail_AndHashedPassword()
    {
        var repository = new FakeUserRepository();
        var service = new AuthService(repository, new FakeCareGroupRepository(), BuildConfiguration());

        var result = await service.RegisterAsync(new RegisterRequest
        {
            Email = "Tester@Example.COM",
            Password = "Password1"
        });

        var savedUser = Assert.Single(repository.Users);
        Assert.Equal("tester@example.com", savedUser.Email);
        Assert.Equal(string.Empty, savedUser.Username);
        Assert.Equal(string.Empty, savedUser.AvatarKey);
        Assert.NotEqual("Password1", savedUser.PasswordHash);
        Assert.True(BCrypt.Net.BCrypt.Verify("Password1", savedUser.PasswordHash));
        Assert.False(result.User.IsProfileCompleted);
    }

    [Fact]
    public async Task CompleteProfileAsync_UpdatesUsernameAndAvatar()
    {
        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = "tester@example.com",
            Username = string.Empty,
            AvatarKey = string.Empty,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("Password1")
        };

        var repository = new FakeUserRepository(user: user);
        var service = new AuthService(repository, new FakeCareGroupRepository(), BuildConfiguration());

        var result = await service.CompleteProfileAsync(user.Id, new CompleteProfileRequest
        {
            Username = "tester",
            AvatarKey = "dog_01"
        });

        Assert.Equal("tester", user.Username);
        Assert.Equal("dog_01", user.AvatarKey);
        Assert.True(result.User.IsProfileCompleted);
    }

    [Fact]
    public async Task LoginAsync_ThrowsUnauthorized_WhenPasswordIsWrong()
    {
        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = "tester@example.com",
            Username = "tester",
            AvatarKey = "dog_01",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("Password1")
        };

        var repository = new FakeUserRepository(user: user);
        var service = new AuthService(repository, new FakeCareGroupRepository(), BuildConfiguration());

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
    public async Task LoginAsync_ThrowsUnauthorized_WhenPasswordCaseDoesNotMatch()
    {
        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = "tester@example.com",
            Username = "tester",
            AvatarKey = "dog_01",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("password")
        };

        var repository = new FakeUserRepository(user: user);
        var service = new AuthService(repository, new FakeCareGroupRepository(), BuildConfiguration());

        var exception = await Assert.ThrowsAsync<DomainException>(() =>
            service.LoginAsync(new LoginRequest
            {
                Email = user.Email,
                Password = "Password"
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
            AvatarKey = "dog_01",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("Password1")
        };

        var repository = new FakeUserRepository(user: user);
        var service = new AuthService(repository, new FakeCareGroupRepository(), BuildConfiguration());

        var result = await service.LoginAsync(new LoginRequest
        {
            Email = "Tester@Example.com",
            Password = "Password1"
        });

        Assert.False(string.IsNullOrWhiteSpace(result.Token));
        Assert.Equal(user.Id, result.User.Id);
        Assert.Equal(user.Email, result.User.Email);
        Assert.Equal(user.Username, result.User.Username);
        Assert.Equal(user.AvatarKey, result.User.AvatarKey);
        Assert.True(result.User.IsProfileCompleted);
        Assert.Equal(0, result.CareGroupCount);
        Assert.Null(result.DefaultCareGroupId);
    }

    [Fact]
    public async Task LoginAsync_ReturnsLastViewedCareGroup_WhenUserHasMultipleGroups()
    {
        var lastViewedCareGroupId = Guid.NewGuid();
        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = "tester@example.com",
            Username = "tester",
            AvatarKey = "dog_01",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("Password1"),
            LastViewedCareGroupId = lastViewedCareGroupId
        };

        var repository = new FakeUserRepository(user: user);
        var careGroupRepository = new FakeCareGroupRepository
        {
            AccessibleCareGroupIds =
            {
                Guid.NewGuid(),
                lastViewedCareGroupId
            }
        };
        var service = new AuthService(repository, careGroupRepository, BuildConfiguration());

        var result = await service.LoginAsync(new LoginRequest
        {
            Email = user.Email,
            Password = "Password1"
        });

        Assert.Equal(2, result.CareGroupCount);
        Assert.Equal(lastViewedCareGroupId, result.DefaultCareGroupId);
    }

    [Fact]
    public async Task UpdateLastViewedCareGroupAsync_ThrowsForbidden_WhenUserIsNotMember()
    {
        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = "tester@example.com",
            Username = "tester",
            AvatarKey = "dog_01",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("Password1")
        };

        var repository = new FakeUserRepository(user: user);
        var service = new AuthService(repository, new FakeCareGroupRepository(), BuildConfiguration());

        var exception = await Assert.ThrowsAsync<DomainException>(() =>
            service.UpdateLastViewedCareGroupAsync(user.Id, Guid.NewGuid()));

        Assert.Equal(403, exception.StatusCode);
        Assert.Equal("FORBIDDEN_ACCESS", exception.ErrorCode);
    }

    [Fact]
    public async Task UpdateLastViewedCareGroupAsync_StoresCareGroupId_WhenUserIsMember()
    {
        var careGroupId = Guid.NewGuid();
        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = "tester@example.com",
            Username = "tester",
            AvatarKey = "dog_01",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("Password1")
        };

        var repository = new FakeUserRepository(user: user);
        var careGroupRepository = new FakeCareGroupRepository();
        careGroupRepository.MemberLookup[(careGroupId, user.Id)] = true;
        var service = new AuthService(repository, careGroupRepository, BuildConfiguration());

        await service.UpdateLastViewedCareGroupAsync(user.Id, careGroupId);

        Assert.Equal(careGroupId, user.LastViewedCareGroupId);
        Assert.NotNull(user.UpdatedAt);
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

        public List<User> Users { get; } = new();

        public FakeUserRepository(bool emailExists = false, User? user = null)
        {
            _emailExists = emailExists;

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

        public Task<User?> GetByIdAsync(Guid id)
        {
            return Task.FromResult(Users.FirstOrDefault(x => x.Id == id));
        }

        public Task AddAsync(User user)
        {
            Users.Add(user);
            return Task.CompletedTask;
        }

        public Task UpdateAsync(User user)
        {
            return Task.CompletedTask;
        }

        public Task SaveChangesAsync()
        {
            return Task.CompletedTask;
        }
    }

    private sealed class FakeCareGroupRepository : ICareGroupRepository
    {
        public List<Guid> AccessibleCareGroupIds { get; } = new();
        public Dictionary<(Guid CareGroupId, Guid UserId), bool> MemberLookup { get; } = new();

        public Task<CareGroup?> GetByIdAsync(Guid id) => Task.FromResult<CareGroup?>(null);
        public Task<CareGroup?> GetByInviteCodeAsync(string inviteCode) => Task.FromResult<CareGroup?>(null);

        public Task<(List<CareGroup> Data, int TotalCount)> GetPagedByUserIdAsync(Guid userId, int page, int pageSize, string sort)
            => Task.FromResult((new List<CareGroup>(), 0));

        public Task<List<Guid>> GetAccessibleCareGroupIdsAsync(Guid userId)
            => Task.FromResult(AccessibleCareGroupIds.ToList());

        public Task AddAsync(CareGroup careGroup) => Task.CompletedTask;

        public Task AddMemberAsync(CareGroupMember member) => Task.CompletedTask;

        public Task<bool> IsMemberAsync(Guid careGroupId, Guid userId)
            => Task.FromResult(MemberLookup.ContainsKey((careGroupId, userId)));

        public Task<CareGroupMember?> GetMemberAsync(Guid careGroupId, Guid userId)
            => Task.FromResult<CareGroupMember?>(null);

        public Task<CareGroupMember?> GetMemberIncludingDeletedAsync(Guid careGroupId, Guid userId)
            => Task.FromResult<CareGroupMember?>(null);

        public Task SaveChangesAsync() => Task.CompletedTask;
    }
}
