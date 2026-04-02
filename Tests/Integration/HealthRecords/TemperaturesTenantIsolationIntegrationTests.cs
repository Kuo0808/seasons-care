using System.Net;
using System.Net.Http.Json;
using SeasonsCare.Api.Models.Entities.HealthRecords;
using SeasonsCare.Api.Tests.Shared;
using SeasonsCare.Api.Tests.Shared.Http;

namespace SeasonsCare.Api.Tests.Integration.HealthRecords;

public class TemperaturesTenantIsolationIntegrationTests
{
    [Fact]
    public async Task GetRecords_ReturnsOnlyRecordsFromRequestedCareGroup()
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
            SeedDataHelper.CreateTemperature(careGroupA.Id, 36.5m),
            SeedDataHelper.CreateTemperature(careGroupB.Id, 38.1m));

        var response = await client.GetAsync($"/api/care-groups/{careGroupA.Id}/health-records/temperatures");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        using var payload = await JsonResponseHelper.ReadJsonAsync(response);
        var items = payload.RootElement.GetProperty("data");
        Assert.Equal(1, items.GetArrayLength());
        Assert.Equal(36.5m, items[0].GetProperty("value").GetDecimal());
    }

    [Fact]
    public async Task CreateRecord_PersistsTemperature_InRequestedCareGroup()
    {
        using var factory = new RealApiFactory();
        using var client = factory.Factory.CreateClient();

        var careGroup = SeedDataHelper.CreateCareGroup("Group A");
        await factory.SeedAsync(
            SeedDataHelper.CreateUser(),
            careGroup,
            SeedDataHelper.CreateMember(careGroup.Id));

        var response = await client.PostAsJsonAsync($"/api/care-groups/{careGroup.Id}/health-records/temperatures", new
        {
            value = 36.7,
            notes = "morning"
        });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        using var payload = await JsonResponseHelper.ReadJsonAsync(response);
        var createdId = payload.RootElement.GetProperty("data").GetProperty("id").GetGuid();
        var saved = await factory.FindAsync<TemperatureRecord>(createdId);

        Assert.NotNull(saved);
        Assert.Equal(careGroup.Id, saved!.CareGroupId);
    }

    [Fact]
    public async Task GetRecordById_ReturnsNotFound_WhenRecordIsInDifferentCareGroup()
    {
        using var factory = new RealApiFactory();
        using var client = factory.Factory.CreateClient();

        var careGroupA = SeedDataHelper.CreateCareGroup("Group A");
        var careGroupB = SeedDataHelper.CreateCareGroup("Group B");
        var recordInGroupB = SeedDataHelper.CreateTemperature(careGroupB.Id, 37.9m);

        await factory.SeedAsync(
            SeedDataHelper.CreateUser(),
            careGroupA,
            careGroupB,
            SeedDataHelper.CreateMember(careGroupA.Id),
            SeedDataHelper.CreateMember(careGroupB.Id),
            recordInGroupB);

        var response = await client.GetAsync($"/api/care-groups/{careGroupA.Id}/health-records/temperatures/{recordInGroupB.Id}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);

        using var payload = await JsonResponseHelper.ReadJsonAsync(response);
        Assert.Equal("NOT_FOUND", payload.RootElement.GetProperty("errorCode").GetString());
    }

    [Fact]
    public async Task UpdateRecord_RefreshesUpdatedAt_AndPersistsChanges()
    {
        using var factory = new RealApiFactory();
        using var client = factory.Factory.CreateClient();

        var careGroup = SeedDataHelper.CreateCareGroup("Group A");
        var existing = SeedDataHelper.CreateTemperature(careGroup.Id, 36.4m);
        await factory.SeedAsync(
            SeedDataHelper.CreateUser(),
            careGroup,
            SeedDataHelper.CreateMember(careGroup.Id),
            existing);
        var persisted = await factory.FindAsync<TemperatureRecord>(existing.Id);

        var response = await client.PutAsJsonAsync($"/api/care-groups/{careGroup.Id}/health-records/temperatures/{existing.Id}", new
        {
            value = 37.1,
            notes = "updated",
            updatedAt = persisted!.UpdatedAt
        });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var saved = await factory.FindAsync<TemperatureRecord>(existing.Id);
        Assert.NotNull(saved);
        Assert.Equal(37.1m, saved!.Value);
        Assert.Equal("updated", saved.Notes);
        Assert.True(saved.UpdatedAt > persisted.UpdatedAt);
    }

    [Fact]
    public async Task DeleteRecord_SoftDeletesRecord_AndHidesItFromList()
    {
        using var factory = new RealApiFactory();
        using var client = factory.Factory.CreateClient();

        var careGroup = SeedDataHelper.CreateCareGroup("Group A");
        var existing = SeedDataHelper.CreateTemperature(careGroup.Id, 36.3m);
        await factory.SeedAsync(
            SeedDataHelper.CreateUser(),
            careGroup,
            SeedDataHelper.CreateMember(careGroup.Id),
            existing);

        var deleteResponse = await client.DeleteAsync($"/api/care-groups/{careGroup.Id}/health-records/temperatures/{existing.Id}");
        Assert.Equal(HttpStatusCode.OK, deleteResponse.StatusCode);

        var allRecords = await factory.GetTemperaturesAsync();
        var deleted = allRecords.Single(x => x.Id == existing.Id);
        Assert.NotNull(deleted.DeletedAt);

        var listResponse = await client.GetAsync($"/api/care-groups/{careGroup.Id}/health-records/temperatures");
        Assert.Equal(HttpStatusCode.OK, listResponse.StatusCode);

        using var payload = await JsonResponseHelper.ReadJsonAsync(listResponse);
        Assert.Equal(0, payload.RootElement.GetProperty("data").GetArrayLength());
    }
}
