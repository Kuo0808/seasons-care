using System.Net;
using System.Net.Http.Json;
using SeasonsCare.Api.Models.Entities.HealthRecords;
using SeasonsCare.Api.Tests.Shared;
using SeasonsCare.Api.Tests.Shared.Http;

namespace SeasonsCare.Api.Tests.Integration.HealthRecords;

public class BloodPressuresTenantIsolationIntegrationTests
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
            SeedDataHelper.CreateBloodPressure(careGroupA.Id, 120, 80),
            SeedDataHelper.CreateBloodPressure(careGroupB.Id, 140, 95));

        var response = await client.GetAsync($"/api/care-groups/{careGroupA.Id}/health-records/blood-pressures");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        using var payload = await JsonResponseHelper.ReadJsonAsync(response);
        var items = payload.RootElement.GetProperty("data");
        Assert.Equal(1, items.GetArrayLength());
        Assert.Equal(120, items[0].GetProperty("systolic").GetInt32());
    }

    [Fact]
    public async Task CreateRecord_PersistsBloodPressure_InRequestedCareGroup()
    {
        using var factory = new RealApiFactory();
        using var client = factory.Factory.CreateClient();

        var careGroup = SeedDataHelper.CreateCareGroup("Group A");
        await factory.SeedAsync(
            SeedDataHelper.CreateUser(),
            careGroup,
            SeedDataHelper.CreateMember(careGroup.Id));

        var response = await client.PostAsJsonAsync($"/api/care-groups/{careGroup.Id}/health-records/blood-pressures", new
        {
            systolic = 118,
            diastolic = 76,
            notes = "morning"
        });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        using var payload = await JsonResponseHelper.ReadJsonAsync(response);
        var createdId = payload.RootElement.GetProperty("data").GetProperty("id").GetGuid();
        var saved = await factory.FindAsync<BloodPressureRecord>(createdId);

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
        var recordInGroupB = SeedDataHelper.CreateBloodPressure(careGroupB.Id, 142, 96);

        await factory.SeedAsync(
            SeedDataHelper.CreateUser(),
            careGroupA,
            careGroupB,
            SeedDataHelper.CreateMember(careGroupA.Id),
            SeedDataHelper.CreateMember(careGroupB.Id),
            recordInGroupB);

        var response = await client.GetAsync($"/api/care-groups/{careGroupA.Id}/health-records/blood-pressures/{recordInGroupB.Id}");

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
        var existing = SeedDataHelper.CreateBloodPressure(careGroup.Id, 120, 80);
        await factory.SeedAsync(
            SeedDataHelper.CreateUser(),
            careGroup,
            SeedDataHelper.CreateMember(careGroup.Id),
            existing);
        var persisted = await factory.FindAsync<BloodPressureRecord>(existing.Id);

        var response = await client.PutAsJsonAsync($"/api/care-groups/{careGroup.Id}/health-records/blood-pressures/{existing.Id}", new
        {
            systolic = 128,
            diastolic = 84,
            notes = "updated",
            updatedAt = persisted!.UpdatedAt
        });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var saved = await factory.FindAsync<BloodPressureRecord>(existing.Id);
        Assert.NotNull(saved);
        Assert.Equal(128, saved!.Systolic);
        Assert.Equal(84, saved.Diastolic);
        Assert.Equal("updated", saved.Notes);
        Assert.True(saved.UpdatedAt > persisted.UpdatedAt);
    }

    [Fact]
    public async Task DeleteRecord_SoftDeletesRecord_AndHidesItFromList()
    {
        using var factory = new RealApiFactory();
        using var client = factory.Factory.CreateClient();

        var careGroup = SeedDataHelper.CreateCareGroup("Group A");
        var existing = SeedDataHelper.CreateBloodPressure(careGroup.Id, 121, 79);
        await factory.SeedAsync(
            SeedDataHelper.CreateUser(),
            careGroup,
            SeedDataHelper.CreateMember(careGroup.Id),
            existing);

        var deleteResponse = await client.DeleteAsync($"/api/care-groups/{careGroup.Id}/health-records/blood-pressures/{existing.Id}");
        Assert.Equal(HttpStatusCode.OK, deleteResponse.StatusCode);

        var allRecords = await factory.GetBloodPressuresAsync();
        var deleted = allRecords.Single(x => x.Id == existing.Id);
        Assert.NotNull(deleted.DeletedAt);

        var listResponse = await client.GetAsync($"/api/care-groups/{careGroup.Id}/health-records/blood-pressures");
        Assert.Equal(HttpStatusCode.OK, listResponse.StatusCode);

        using var payload = await JsonResponseHelper.ReadJsonAsync(listResponse);
        Assert.Equal(0, payload.RootElement.GetProperty("data").GetArrayLength());
    }
}
