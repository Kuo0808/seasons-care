using System.Net;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SeasonsCare.Api.Data;
using SeasonsCare.Api.Models.Entities;
using SeasonsCare.Api.Models.Enums;
using SeasonsCare.Api.Tests.Shared;
using SeasonsCare.Api.Tests.Shared.Http;

namespace SeasonsCare.Api.Tests.Integration.CareGroups;

public class CareGroupsTenantIsolationIntegrationTests
{
    [Fact]
    public async Task GetMyGroups_ReturnsOnlyGroupsForCurrentUser()
    {
        using var factory = new RealApiFactory();
        using var client = factory.Factory.CreateClient();

        var currentUser = SeedDataHelper.CreateUser();
        var otherUserId = Guid.Parse("22222222-2222-2222-2222-222222222222");
        var otherUser = SeedDataHelper.CreateUser(otherUserId);
        var careGroupA = SeedDataHelper.CreateCareGroup("Group A");
        var careGroupB = SeedDataHelper.CreateCareGroup("Group B");

        await factory.SeedAsync(
            currentUser,
            otherUser,
            careGroupA,
            careGroupB,
            SeedDataHelper.CreateMember(careGroupA.Id, currentUser.Id),
            SeedDataHelper.CreateMember(careGroupB.Id, otherUser.Id));

        var response = await client.GetAsync("/api/care-groups");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        using var payload = await JsonResponseHelper.ReadJsonAsync(response);
        var items = payload.RootElement.GetProperty("data");
        Assert.Single(items.EnumerateArray());
        Assert.Equal("Group A", items[0].GetProperty("name").GetString());
    }

    [Fact]
    public async Task GetMyGroups_SortsAscending_WhenRequested()
    {
        using var factory = new RealApiFactory();
        using var client = factory.Factory.CreateClient();

        var currentUser = SeedDataHelper.CreateUser();
        var olderGroup = SeedDataHelper.CreateCareGroup("Older Group");
        olderGroup.CreatedAt = new DateTime(2026, 3, 20, 0, 0, 0, DateTimeKind.Utc);
        var newerGroup = SeedDataHelper.CreateCareGroup("Newer Group");
        newerGroup.CreatedAt = new DateTime(2026, 3, 21, 0, 0, 0, DateTimeKind.Utc);

        await factory.SeedAsync(
            currentUser,
            olderGroup,
            newerGroup,
            SeedDataHelper.CreateMember(olderGroup.Id, currentUser.Id),
            SeedDataHelper.CreateMember(newerGroup.Id, currentUser.Id));

        var response = await client.GetAsync("/api/care-groups?sort=createdAt_asc");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        using var payload = await JsonResponseHelper.ReadJsonAsync(response);
        var items = payload.RootElement.GetProperty("data").EnumerateArray().ToList();
        Assert.Equal(2, items.Count);
        Assert.Equal("Older Group", items[0].GetProperty("name").GetString());
        Assert.Equal("Newer Group", items[1].GetProperty("name").GetString());
    }

    [Fact]
    public async Task CreateCareGroup_PersistsGroup_AndCreatorMembership()
    {
        using var factory = new RealApiFactory();
        using var client = factory.Factory.CreateClient();

        await factory.SeedAsync(SeedDataHelper.CreateUser());

        var response = await client.PostAsJsonAsync("/api/care-groups", new
        {
            name = "Home Care",
            recipientName = "Dad",
            recipientGender = "Male",
            recipientBirthDate = "1950-01-02"
        });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        using var payload = await JsonResponseHelper.ReadJsonAsync(response);
        var createdId = payload.RootElement.GetProperty("data").GetProperty("id").GetGuid();

        using var scope = factory.Factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var savedGroup = await dbContext.CareGroups.FirstOrDefaultAsync(x => x.Id == createdId);
        var membership = await dbContext.CareGroupMembers.FirstOrDefaultAsync(x => x.CareGroupId == createdId && x.UserId == TestUsers.DefaultUserId);

        Assert.NotNull(savedGroup);
        Assert.Equal("Home Care", savedGroup!.Name);
        Assert.Equal("Dad", savedGroup.RecipientName);
        Assert.Equal("Male", savedGroup.RecipientGender);
        Assert.NotNull(membership);
        Assert.Equal(CareGroupRole.Admin, membership!.Role);
    }

    [Fact]
    public async Task JoinCareGroup_AddsMembership_WhenInviteCodeMatches()
    {
        using var factory = new RealApiFactory();
        using var client = factory.Factory.CreateClient();

        var careGroup = SeedDataHelper.CreateCareGroup("Group A");
        careGroup.InviteCode = "JOIN1234";

        await factory.SeedAsync(
            SeedDataHelper.CreateUser(),
            careGroup);

        var response = await client.PostAsJsonAsync($"/api/care-groups/{careGroup.Id}/members", new
        {
            inviteCode = "JOIN1234"
        });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        using var scope = factory.Factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var membership = await dbContext.CareGroupMembers.FirstOrDefaultAsync(x => x.CareGroupId == careGroup.Id && x.UserId == TestUsers.DefaultUserId);

        Assert.NotNull(membership);
        Assert.Equal(CareGroupRole.Member, membership!.Role);
    }

    [Fact]
    public async Task JoinCareGroupByInviteCode_AddsMembership_WhenInviteCodeMatches()
    {
        using var factory = new RealApiFactory();
        using var client = factory.Factory.CreateClient();

        var careGroup = SeedDataHelper.CreateCareGroup("Group A");
        careGroup.InviteCode = "JOIN1234";

        await factory.SeedAsync(
            SeedDataHelper.CreateUser(),
            careGroup);

        var response = await client.PostAsJsonAsync("/api/care-groups/join", new
        {
            inviteCode = "JOIN1234"
        });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        using var scope = factory.Factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var membership = await dbContext.CareGroupMembers.FirstOrDefaultAsync(x => x.CareGroupId == careGroup.Id && x.UserId == TestUsers.DefaultUserId);

        Assert.NotNull(membership);
        Assert.Equal(CareGroupRole.Member, membership!.Role);
    }

    [Fact]
    public async Task JoinCareGroup_RestoresSoftDeletedMembership_WhenUserRejoins()
    {
        using var factory = new RealApiFactory();
        using var client = factory.Factory.CreateClient();

        var careGroup = SeedDataHelper.CreateCareGroup("Group A");
        careGroup.InviteCode = "JOIN1234";
        var deletedMembership = SeedDataHelper.CreateMember(careGroup.Id, TestUsers.DefaultUserId, CareGroupRole.Member);
        deletedMembership.DeletedAt = DateTime.UtcNow.AddDays(-1);

        await factory.SeedAsync(
            SeedDataHelper.CreateUser(),
            careGroup,
            deletedMembership);

        var response = await client.PostAsJsonAsync($"/api/care-groups/{careGroup.Id}/members", new
        {
            inviteCode = "JOIN1234"
        });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        using var scope = factory.Factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var memberships = await dbContext.CareGroupMembers
            .IgnoreQueryFilters()
            .Where(x => x.CareGroupId == careGroup.Id && x.UserId == TestUsers.DefaultUserId)
            .ToListAsync();

        Assert.Single(memberships);
        Assert.Null(memberships[0].DeletedAt);
    }

    [Fact]
    public async Task RemoveMember_SoftDeletesMembership_WhenAdminRemovesTarget()
    {
        using var factory = new RealApiFactory();
        using var client = factory.Factory.CreateClient();

        var memberUserId = Guid.Parse("33333333-3333-3333-3333-333333333333");
        var careGroup = SeedDataHelper.CreateCareGroup("Group A");
        var adminMembership = SeedDataHelper.CreateMember(careGroup.Id, TestUsers.DefaultUserId, CareGroupRole.Admin);
        var memberToRemove = SeedDataHelper.CreateMember(careGroup.Id, memberUserId, CareGroupRole.Member);

        await factory.SeedAsync(
            SeedDataHelper.CreateUser(),
            SeedDataHelper.CreateUser(memberUserId),
            careGroup,
            adminMembership,
            memberToRemove);

        var response = await client.DeleteAsync($"/api/care-groups/{careGroup.Id}/members/{memberUserId}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        using var scope = factory.Factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var membership = await dbContext.CareGroupMembers.IgnoreQueryFilters().FirstAsync(x => x.Id == memberToRemove.Id);

        Assert.NotNull(membership.DeletedAt);
    }
}
