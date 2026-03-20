using System.Net;
using System.Net.Http.Json;
using SeasonsCare.Api.Models.Entities;
using SeasonsCare.Api.Tests.Shared;
using SeasonsCare.Api.Tests.Shared.Http;

namespace SeasonsCare.Api.Tests.Integration.CareLogs;

public class CareLogsTenantIsolationIntegrationTests
{
    [Fact]
    public async Task GetLogs_ReturnsOnlyLogsFromRequestedCareGroup()
    {
        using var factory = new RealApiFactory();
        using var client = factory.Factory.CreateClient();

        var careGroupA = SeedDataHelper.CreateCareGroup("Group A");
        var careGroupB = SeedDataHelper.CreateCareGroup("Group B");
        await factory.SeedAsync(
            SeedDataHelper.CreateUser(),
            careGroupA,
            careGroupB,
            SeedDataHelper.CreateMember(careGroupA.Id),
            SeedDataHelper.CreateMember(careGroupB.Id),
            SeedDataHelper.CreateCareLog(careGroupA.Id, "A1"),
            SeedDataHelper.CreateCareLog(careGroupB.Id, "B1"));

        var response = await client.GetAsync($"/api/care-groups/{careGroupA.Id}/care-logs");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        using var payload = await JsonResponseHelper.ReadJsonAsync(response);
        var items = payload.RootElement.GetProperty("data");
        Assert.Equal(1, items.GetArrayLength());
        Assert.Equal("A1", items[0].GetProperty("title").GetString());
    }

    [Fact]
    public async Task GetLogById_ReturnsNotFound_WhenLogIsInDifferentCareGroup()
    {
        using var factory = new RealApiFactory();
        using var client = factory.Factory.CreateClient();

        var careGroupA = SeedDataHelper.CreateCareGroup("Group A");
        var careGroupB = SeedDataHelper.CreateCareGroup("Group B");
        var logInGroupB = SeedDataHelper.CreateCareLog(careGroupB.Id, "B1");

        await factory.SeedAsync(
            SeedDataHelper.CreateUser(),
            careGroupA,
            careGroupB,
            SeedDataHelper.CreateMember(careGroupA.Id),
            SeedDataHelper.CreateMember(careGroupB.Id),
            logInGroupB);

        var response = await client.GetAsync($"/api/care-groups/{careGroupA.Id}/care-logs/{logInGroupB.Id}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);

        using var payload = await JsonResponseHelper.ReadJsonAsync(response);
        Assert.Equal("NOT_FOUND", payload.RootElement.GetProperty("errorCode").GetString());
    }

    [Fact]
    public async Task CreateLog_PersistsCareLog_InRequestedCareGroup()
    {
        using var factory = new RealApiFactory();
        using var client = factory.Factory.CreateClient();
        var recordDate = DateTime.UtcNow.AddMinutes(-1);

        var careGroup = SeedDataHelper.CreateCareGroup("Group A");
        await factory.SeedAsync(
            SeedDataHelper.CreateUser(),
            careGroup,
            SeedDataHelper.CreateMember(careGroup.Id));

        var response = await client.PostAsJsonAsync($"/api/care-groups/{careGroup.Id}/care-logs", new
        {
            title = "Created from API",
            content = "content",
            logType = "Daily",
            recordDate
        });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        using var payload = await JsonResponseHelper.ReadJsonAsync(response);
        var data = payload.RootElement.GetProperty("data");
        var createdId = data.GetProperty("id").GetGuid();

        var saved = await factory.FindAsync<CareLog>(createdId);
        Assert.NotNull(saved);
        Assert.Equal(careGroup.Id, saved!.CareGroupId);
        Assert.Equal("Created from API", saved.Title);
        Assert.NotNull(saved.UpdatedAt);
    }

    [Fact]
    public async Task UpdateLog_RefreshesUpdatedAt_AndPersistsChanges()
    {
        using var factory = new RealApiFactory();
        using var client = factory.Factory.CreateClient();
        var updatedRecordDate = DateTime.UtcNow.AddMinutes(-1);

        var careGroup = SeedDataHelper.CreateCareGroup("Group A");
        var existingLog = SeedDataHelper.CreateCareLog(careGroup.Id, "Before Update");
        await factory.SeedAsync(
            SeedDataHelper.CreateUser(),
            careGroup,
            SeedDataHelper.CreateMember(careGroup.Id),
            existingLog);
        var persistedLog = await factory.FindAsync<CareLog>(existingLog.Id);

        var response = await client.PutAsJsonAsync($"/api/care-groups/{careGroup.Id}/care-logs/{existingLog.Id}", new
        {
            title = "After Update",
            content = "updated content",
            logType = "Medical",
            recordDate = updatedRecordDate,
            updatedAt = persistedLog!.UpdatedAt!.Value
        });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var saved = await factory.FindAsync<CareLog>(existingLog.Id);
        Assert.NotNull(saved);
        Assert.Equal("After Update", saved!.Title);
        Assert.Equal("updated content", saved.Content);
        Assert.Equal("Medical", saved.LogType);
        Assert.Equal(updatedRecordDate, saved.RecordDate);
        Assert.True(saved.UpdatedAt > persistedLog.UpdatedAt);
    }

    [Fact]
    public async Task DeleteLog_SoftDeletesLog_AndHidesItFromList()
    {
        using var factory = new RealApiFactory();
        using var client = factory.Factory.CreateClient();

        var careGroup = SeedDataHelper.CreateCareGroup("Group A");
        var existingLog = SeedDataHelper.CreateCareLog(careGroup.Id, "Delete Me");
        await factory.SeedAsync(
            SeedDataHelper.CreateUser(),
            careGroup,
            SeedDataHelper.CreateMember(careGroup.Id),
            existingLog);

        var deleteResponse = await client.DeleteAsync($"/api/care-groups/{careGroup.Id}/care-logs/{existingLog.Id}");
        Assert.Equal(HttpStatusCode.OK, deleteResponse.StatusCode);

        var allLogs = await factory.GetCareLogsAsync();
        var deleted = allLogs.Single(x => x.Id == existingLog.Id);
        Assert.NotNull(deleted.DeletedAt);

        var listResponse = await client.GetAsync($"/api/care-groups/{careGroup.Id}/care-logs");
        Assert.Equal(HttpStatusCode.OK, listResponse.StatusCode);

        using var payload = await JsonResponseHelper.ReadJsonAsync(listResponse);
        var items = payload.RootElement.GetProperty("data");
        Assert.Equal(0, items.GetArrayLength());
    }
}
