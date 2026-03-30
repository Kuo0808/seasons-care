using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SeasonsCare.Api.Data;
using SeasonsCare.Api.Tests.Shared;
using SeasonsCare.Api.Tests.Shared.Http;

namespace SeasonsCare.Api.Tests.Integration.Auth;

public class AuthOnboardingRealIntegrationTests
{
    [Fact]
    public async Task Register_UpdateProfile_AndLogin_WorkEndToEnd()
    {
        using var factory = new RealApiFactory();
        using var client = factory.Factory.CreateClient();

        var registerResponse = await client.PostAsJsonAsync("/api/auth/register", new
        {
            email = "new-user@example.com",
            password = "Password1"
        });

        Assert.Equal(HttpStatusCode.Created, registerResponse.StatusCode);

        using var registerPayload = await JsonDocument.ParseAsync(await registerResponse.Content.ReadAsStreamAsync());
        var registerData = registerPayload.RootElement.GetProperty("data");
        var token = registerData.GetProperty("token").GetString();
        var userId = registerData.GetProperty("user").GetProperty("id").GetGuid();
        var isProfileCompleted = registerData.GetProperty("user").GetProperty("isProfileCompleted").GetBoolean();
        var registerCareGroupCount = registerData.GetProperty("careGroupCount").GetInt32();

        Assert.False(isProfileCompleted);
        Assert.False(string.IsNullOrWhiteSpace(token));
        Assert.Equal(0, registerCareGroupCount);
        Assert.True(registerData.GetProperty("defaultCareGroupId").ValueKind == JsonValueKind.Null);

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        client.DefaultRequestHeaders.Remove("X-Test-UserId");
        client.DefaultRequestHeaders.Add("X-Test-UserId", userId.ToString());

        var updateResponse = await client.PatchAsJsonAsync("/api/users/me", new
        {
            userName = "tester",
            avatarKey = "dog_01"
        });

        Assert.Equal(HttpStatusCode.OK, updateResponse.StatusCode);

        using var updatePayload = await JsonDocument.ParseAsync(await updateResponse.Content.ReadAsStreamAsync());
        var updatedUser = updatePayload.RootElement.GetProperty("data").GetProperty("user");
        Assert.Equal("tester", updatedUser.GetProperty("userName").GetString());
        Assert.Equal("dog_01", updatedUser.GetProperty("avatarKey").GetString());
        Assert.True(updatedUser.GetProperty("isProfileCompleted").GetBoolean());

        client.DefaultRequestHeaders.Authorization = null;
        client.DefaultRequestHeaders.Remove("X-Test-UserId");

        var loginResponse = await client.PostAsJsonAsync("/api/auth/login", new
        {
            email = "new-user@example.com",
            password = "Password1"
        });

        Assert.Equal(HttpStatusCode.OK, loginResponse.StatusCode);

        using var loginPayload = await JsonDocument.ParseAsync(await loginResponse.Content.ReadAsStreamAsync());
        var loginData = loginPayload.RootElement.GetProperty("data");
        var loginUser = loginData.GetProperty("user");
        Assert.Equal("tester", loginUser.GetProperty("userName").GetString());
        Assert.Equal("dog_01", loginUser.GetProperty("avatarKey").GetString());
        Assert.True(loginUser.GetProperty("isProfileCompleted").GetBoolean());
        Assert.Equal(0, loginData.GetProperty("careGroupCount").GetInt32());
        Assert.True(loginData.GetProperty("defaultCareGroupId").ValueKind == JsonValueKind.Null);

        using var scope = factory.Factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var savedUser = await dbContext.Users.FirstAsync(x => x.Id == userId);
        Assert.Equal("new-user@example.com", savedUser.Email);
        Assert.Equal("tester", savedUser.Username);
        Assert.Equal("dog_01", savedUser.AvatarKey);
    }

    [Fact]
    public async Task Login_ReturnsLastViewedCareGroup_WhenUserHasMultipleGroups()
    {
        using var factory = new RealApiFactory();
        using var scope = factory.Factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        var userId = Guid.NewGuid();
        var careGroupA = SeedDataHelper.CreateCareGroup("Group A");
        var careGroupB = SeedDataHelper.CreateCareGroup("Group B");
        var user = SeedDataHelper.CreateUser(userId);
        user.Email = "member@example.com";
        user.PasswordHash = BCrypt.Net.BCrypt.HashPassword("Password1");
        user.LastViewedCareGroupId = careGroupB.Id;

        dbContext.Users.Add(user);
        dbContext.CareGroups.AddRange(careGroupA, careGroupB);
        dbContext.CareGroupMembers.AddRange(
            SeedDataHelper.CreateMember(careGroupA.Id, userId),
            SeedDataHelper.CreateMember(careGroupB.Id, userId));
        await dbContext.SaveChangesAsync();

        using var client = factory.Factory.CreateClient();

        var loginResponse = await client.PostAsJsonAsync("/api/auth/login", new
        {
            email = "member@example.com",
            password = "Password1"
        });

        Assert.Equal(HttpStatusCode.OK, loginResponse.StatusCode);

        using var loginPayload = await JsonDocument.ParseAsync(await loginResponse.Content.ReadAsStreamAsync());
        var loginData = loginPayload.RootElement.GetProperty("data");
        Assert.Equal(2, loginData.GetProperty("careGroupCount").GetInt32());
        Assert.Equal(careGroupB.Id, loginData.GetProperty("defaultCareGroupId").GetGuid());

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", "stub-token");
        client.DefaultRequestHeaders.Remove("X-Test-UserId");
        client.DefaultRequestHeaders.Add("X-Test-UserId", userId.ToString());

        var updateResponse = await client.PatchAsJsonAsync("/api/users/me/last-viewed-care-group", new
        {
            careGroupId = careGroupA.Id
        });

        Assert.Equal(HttpStatusCode.OK, updateResponse.StatusCode);

        dbContext.ChangeTracker.Clear();
        var savedUser = await dbContext.Users.FirstAsync(x => x.Id == userId);
        Assert.Equal(careGroupA.Id, savedUser.LastViewedCareGroupId);
    }
}
