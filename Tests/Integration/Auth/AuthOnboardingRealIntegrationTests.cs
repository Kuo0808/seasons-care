using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SeasonsCare.Api.Data;
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

        Assert.False(isProfileCompleted);
        Assert.False(string.IsNullOrWhiteSpace(token));

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        client.DefaultRequestHeaders.Remove("X-Test-UserId");
        client.DefaultRequestHeaders.Add("X-Test-UserId", userId.ToString());

        var updateResponse = await client.PatchAsJsonAsync("/api/users/me", new
        {
            username = "tester",
            avatarKey = "dog_01"
        });

        Assert.Equal(HttpStatusCode.OK, updateResponse.StatusCode);

        using var updatePayload = await JsonDocument.ParseAsync(await updateResponse.Content.ReadAsStreamAsync());
        var updatedUser = updatePayload.RootElement.GetProperty("data").GetProperty("user");
        Assert.Equal("tester", updatedUser.GetProperty("username").GetString());
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
        var loginUser = loginPayload.RootElement.GetProperty("data").GetProperty("user");
        Assert.Equal("tester", loginUser.GetProperty("username").GetString());
        Assert.Equal("dog_01", loginUser.GetProperty("avatarKey").GetString());
        Assert.True(loginUser.GetProperty("isProfileCompleted").GetBoolean());

        using var scope = factory.Factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var savedUser = await dbContext.Users.FirstAsync(x => x.Id == userId);
        Assert.Equal("new-user@example.com", savedUser.Email);
        Assert.Equal("tester", savedUser.Username);
        Assert.Equal("dog_01", savedUser.AvatarKey);
    }
}
