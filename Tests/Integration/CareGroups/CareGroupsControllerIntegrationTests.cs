using System.Net;
using System.Net.Http.Json;
using SeasonsCare.Api.DTOs.CareGroups;
using SeasonsCare.Api.Exceptions;
using SeasonsCare.Api.Services;
using SeasonsCare.Api.Tests.Shared;
using SeasonsCare.Api.Tests.Shared.Http;

namespace SeasonsCare.Api.Tests.Integration.CareGroups;

public class CareGroupsControllerIntegrationTests
{
    [Fact]
    public async Task CreateCareGroup_ReturnsBadRequest_WhenRecipientGenderIsMissing()
    {
        using var factory = new StubApiFactory<ICareGroupService>(new StubCareGroupService());
        using var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/care-groups", new
        {
            name = "Home Care",
            recipientName = "Dad",
            recipientGender = "",
            recipientBirthDate = "1950-01-02"
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        using var payload = await JsonResponseHelper.ReadJsonAsync(response);
        Assert.Equal("VALIDATION_FAILED", payload.RootElement.GetProperty("errorCode").GetString());
    }

    [Fact]
    public async Task GetCareGroup_ReturnsForbidden_WhenServiceRejectsAccess()
    {
        using var factory = new StubApiFactory<ICareGroupService>(new StubCareGroupService
        {
            GetByIdException = new DomainException("forbidden", "FORBIDDEN_ACCESS", 403)
        });
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/api/care-groups/aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);

        using var payload = await JsonResponseHelper.ReadJsonAsync(response);
        Assert.Equal("FORBIDDEN_ACCESS", payload.RootElement.GetProperty("errorCode").GetString());
    }

    [Fact]
    public async Task JoinCareGroup_ReturnsConflict_WhenServiceDetectsDuplicateMembership()
    {
        using var factory = new StubApiFactory<ICareGroupService>(new StubCareGroupService
        {
            JoinException = new DomainException("conflict", "CONFLICT", 409)
        });
        using var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/care-groups/aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa/members", new
        {
            inviteCode = "JOIN1234"
        });

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);

        using var payload = await JsonResponseHelper.ReadJsonAsync(response);
        Assert.Equal("CONFLICT", payload.RootElement.GetProperty("errorCode").GetString());
    }

    [Fact]
    public async Task JoinCareGroupByInviteCode_ReturnsConflict_WhenServiceDetectsDuplicateMembership()
    {
        using var factory = new StubApiFactory<ICareGroupService>(new StubCareGroupService
        {
            JoinByInviteCodeException = new DomainException("conflict", "CONFLICT", 409)
        });
        using var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/care-groups/join", new
        {
            inviteCode = "JOIN1234"
        });

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);

        using var payload = await JsonResponseHelper.ReadJsonAsync(response);
        Assert.Equal("CONFLICT", payload.RootElement.GetProperty("errorCode").GetString());
    }

    private sealed class StubCareGroupService : ICareGroupService
    {
        public Exception? GetByIdException { get; init; }
        public Exception? JoinException { get; init; }
        public Exception? JoinByInviteCodeException { get; init; }

        public Task<CareGroupResponse> CreateAsync(Guid currentUserId, CreateCareGroupRequest request)
        {
            return Task.FromResult(new CareGroupResponse
            {
                Id = Guid.NewGuid(),
                Name = request.Name,
                RecipientName = request.RecipientName,
                RecipientGender = request.RecipientGender,
                RecipientBirthDate = request.RecipientBirthDate,
                InviteCode = "JOIN1234",
                MemberCount = 1
            });
        }

        public Task<SeasonsCare.Api.DTOs.Common.PagedResponse<CareGroupResponse>> GetMyGroupsAsync(Guid currentUserId, SeasonsCare.Api.DTOs.Common.PaginationRequest pagination)
        {
            throw new NotImplementedException();
        }

        public Task<CareGroupDetailResponse> GetByIdAsync(Guid currentUserId, Guid careGroupId)
        {
            if (GetByIdException is not null)
            {
                throw GetByIdException;
            }

            return Task.FromResult(new CareGroupDetailResponse
            {
                Id = careGroupId,
                Name = "Home Care",
                RecipientName = "Dad",
                RecipientGender = "Male",
                RecipientBirthDate = new DateOnly(1950, 1, 2),
                InviteCode = "JOIN1234"
            });
        }

        public Task<CareGroupResponse> UpdateAsync(Guid currentUserId, Guid careGroupId, UpdateCareGroupRequest request)
        {
            return Task.FromResult(new CareGroupResponse
            {
                Id = careGroupId,
                Name = request.Name,
                RecipientName = request.RecipientName,
                RecipientGender = request.RecipientGender,
                RecipientBirthDate = request.RecipientBirthDate,
                InviteCode = "JOIN1234",
                MemberCount = 1
            });
        }

        public Task JoinByInviteCodeAsync(Guid currentUserId, JoinCareGroupRequest request)
        {
            if (JoinByInviteCodeException is not null)
            {
                throw JoinByInviteCodeException;
            }

            return Task.CompletedTask;
        }

        public Task JoinAsync(Guid currentUserId, Guid careGroupId, JoinCareGroupRequest request)
        {
            if (JoinException is not null)
            {
                throw JoinException;
            }

            return Task.CompletedTask;
        }

        public Task RemoveMemberAsync(Guid currentUserId, Guid careGroupId, Guid memberUserId)
        {
            return Task.CompletedTask;
        }
    }
}
