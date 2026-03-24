using System.Net;
using System.Net.Http.Json;
using SeasonsCare.Api.Models.Entities.HealthRecords;
using SeasonsCare.Api.Tests.Shared;
using SeasonsCare.Api.Tests.Shared.Http;

namespace SeasonsCare.Api.Tests.Integration.HealthRecords;

public class BloodSugarsTenantIsolationIntegrationTests
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
            SeedDataHelper.CreateBloodSugar(careGroupA.Id, 110m, "飯前"),
            SeedDataHelper.CreateBloodSugar(careGroupB.Id, 145m, "飯後"));

        var response = await client.GetAsync($"/api/care-groups/{careGroupA.Id}/health-records/blood-sugars");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        using var payload = await JsonResponseHelper.ReadJsonAsync(response);
        var items = payload.RootElement.GetProperty("data");
        Assert.Equal(1, items.GetArrayLength());
        Assert.Equal(110m, items[0].GetProperty("glucoseLevel").GetDecimal());
    }

    [Fact]
    public async Task CreateRecord_PersistsBloodSugar_InRequestedCareGroup()
    {
        using var factory = new RealApiFactory();
        using var client = factory.Factory.CreateClient();

        var careGroup = SeedDataHelper.CreateCareGroup("Group A");
        await factory.SeedAsync(
            SeedDataHelper.CreateUser(),
            careGroup,
            SeedDataHelper.CreateMember(careGroup.Id));

        var response = await client.PostAsJsonAsync($"/api/care-groups/{careGroup.Id}/health-records/blood-sugars", new
        {
            glucoseLevel = 123,
            measurementContext = "飯前",
            notes = "fasting"
        });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        using var payload = await JsonResponseHelper.ReadJsonAsync(response);
        var createdId = payload.RootElement.GetProperty("data").GetProperty("id").GetGuid();
        var saved = await factory.FindAsync<BloodSugarRecord>(createdId);

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
        var recordInGroupB = SeedDataHelper.CreateBloodSugar(careGroupB.Id, 155m, "飯後");

        await factory.SeedAsync(
            SeedDataHelper.CreateUser(),
            careGroupA,
            careGroupB,
            SeedDataHelper.CreateMember(careGroupA.Id),
            SeedDataHelper.CreateMember(careGroupB.Id),
            recordInGroupB);

        var response = await client.GetAsync($"/api/care-groups/{careGroupA.Id}/health-records/blood-sugars/{recordInGroupB.Id}");

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
        var existing = SeedDataHelper.CreateBloodSugar(careGroup.Id, 110m, "飯前");
        await factory.SeedAsync(
            SeedDataHelper.CreateUser(),
            careGroup,
            SeedDataHelper.CreateMember(careGroup.Id),
            existing);
        var persisted = await factory.FindAsync<BloodSugarRecord>(existing.Id);

        var response = await client.PutAsJsonAsync($"/api/care-groups/{careGroup.Id}/health-records/blood-sugars/{existing.Id}", new
        {
            glucoseLevel = 132,
            measurementContext = "飯後",
            notes = "updated",
            updatedAt = persisted!.UpdatedAt
        });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var saved = await factory.FindAsync<BloodSugarRecord>(existing.Id);
        Assert.NotNull(saved);
        Assert.Equal(132m, saved!.GlucoseLevel);
        Assert.Equal("飯後", saved.MeasurementContext);
        Assert.Equal("updated", saved.Notes);
        Assert.True(saved.UpdatedAt > persisted.UpdatedAt);
    }

    [Fact]
    public async Task DeleteRecord_SoftDeletesRecord_AndHidesItFromList()
    {
        using var factory = new RealApiFactory();
        using var client = factory.Factory.CreateClient();

        var careGroup = SeedDataHelper.CreateCareGroup("Group A");
        var existing = SeedDataHelper.CreateBloodSugar(careGroup.Id, 111m, "飯前");
        await factory.SeedAsync(
            SeedDataHelper.CreateUser(),
            careGroup,
            SeedDataHelper.CreateMember(careGroup.Id),
            existing);

        var deleteResponse = await client.DeleteAsync($"/api/care-groups/{careGroup.Id}/health-records/blood-sugars/{existing.Id}");
        Assert.Equal(HttpStatusCode.OK, deleteResponse.StatusCode);

        var allRecords = await factory.GetBloodSugarsAsync();
        var deleted = allRecords.Single(x => x.Id == existing.Id);
        Assert.NotNull(deleted.DeletedAt);

        var listResponse = await client.GetAsync($"/api/care-groups/{careGroup.Id}/health-records/blood-sugars");
        Assert.Equal(HttpStatusCode.OK, listResponse.StatusCode);

        using var payload = await JsonResponseHelper.ReadJsonAsync(listResponse);
        Assert.Equal(0, payload.RootElement.GetProperty("data").GetArrayLength());
    }
}
