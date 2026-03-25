using System.Net;
using System.Net.Http.Json;
using SeasonsCare.Api.Models.Entities.HealthRecords;
using SeasonsCare.Api.Tests.Shared;
using SeasonsCare.Api.Tests.Shared.Http;

namespace SeasonsCare.Api.Tests.Integration.HealthRecords;

public class BloodOxygensTenantIsolationIntegrationTests
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
            SeedDataHelper.CreateBloodOxygen(careGroupA.Id, 98m),
            SeedDataHelper.CreateBloodOxygen(careGroupB.Id, 95m));

        var response = await client.GetAsync($"/api/care-groups/{careGroupA.Id}/health-records/blood-oxygens");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        using var payload = await JsonResponseHelper.ReadJsonAsync(response);
        var items = payload.RootElement.GetProperty("data");
        Assert.Equal(1, items.GetArrayLength());
        Assert.Equal(98m, items[0].GetProperty("spO2").GetDecimal());
    }

    [Fact]
    public async Task GetRecordById_ReturnsNotFound_WhenRecordIsInDifferentCareGroup()
    {
        using var factory = new RealApiFactory();
        using var client = factory.Factory.CreateClient();

        var careGroupA = SeedDataHelper.CreateCareGroup("Group A");
        var careGroupB = SeedDataHelper.CreateCareGroup("Group B");
        var recordInGroupB = SeedDataHelper.CreateBloodOxygen(careGroupB.Id, 96m);

        await factory.SeedAsync(
            SeedDataHelper.CreateUser(),
            careGroupA,
            careGroupB,
            SeedDataHelper.CreateMember(careGroupA.Id),
            SeedDataHelper.CreateMember(careGroupB.Id),
            recordInGroupB);

        var response = await client.GetAsync($"/api/care-groups/{careGroupA.Id}/health-records/blood-oxygens/{recordInGroupB.Id}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);

        using var payload = await JsonResponseHelper.ReadJsonAsync(response);
        Assert.Equal("NOT_FOUND", payload.RootElement.GetProperty("errorCode").GetString());
    }

    [Fact]
    public async Task CreateRecord_PersistsBloodOxygen_InRequestedCareGroup()
    {
        using var factory = new RealApiFactory();
        using var client = factory.Factory.CreateClient();

        var careGroup = SeedDataHelper.CreateCareGroup("Group A");
        await factory.SeedAsync(
            SeedDataHelper.CreateUser(),
            careGroup,
            SeedDataHelper.CreateMember(careGroup.Id));

        var response = await client.PostAsJsonAsync($"/api/care-groups/{careGroup.Id}/health-records/blood-oxygens", new
        {
            spO2 = 98,
            notes = "resting"
        });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        using var payload = await JsonResponseHelper.ReadJsonAsync(response);
        var createdId = payload.RootElement.GetProperty("data").GetProperty("id").GetGuid();
        var saved = await factory.FindAsync<BloodOxygenRecord>(createdId);

        Assert.NotNull(saved);
        Assert.Equal(careGroup.Id, saved!.CareGroupId);
    }
}
