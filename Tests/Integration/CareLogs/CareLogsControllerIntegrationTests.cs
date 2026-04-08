using System.Net;
using System.Net.Http.Json;
using SeasonsCare.Api.DTOs.CareLogs;
using SeasonsCare.Api.Exceptions;
using SeasonsCare.Api.Services;
using SeasonsCare.Api.Tests.Shared;
using SeasonsCare.Api.Tests.Shared.Http;

namespace SeasonsCare.Api.Tests.Integration.CareLogs;

public class CareLogsControllerIntegrationTests
{
    [Fact]
    public async Task CreateLog_ReturnsBadRequest_WhenTitleIsMissing()
    {
        using var factory = new StubApiFactory<ICareLogService>(new StubCareLogService());
        using var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/care-groups/aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa/care-logs", new
        {
            title = "",
            description = "note"
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        var payload = await JsonResponseHelper.ReadJsonAsync(response);
        Assert.Equal("VALIDATION_FAILED", payload.RootElement.GetProperty("errorCode").GetString());
    }

    [Fact]
    public async Task UpdateLog_ReturnsBadRequest_WhenUpdatedAtIsMissing()
    {
        using var factory = new StubApiFactory<ICareLogService>(new StubCareLogService());
        using var client = factory.CreateClient();

        var response = await client.PutAsJsonAsync("/api/care-groups/aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa/care-logs/bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb", new
        {
            title = "updated title"
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        var payload = await JsonResponseHelper.ReadJsonAsync(response);
        Assert.Equal("VALIDATION_FAILED", payload.RootElement.GetProperty("errorCode").GetString());
    }

    [Fact]
    public async Task GetLog_ReturnsForbidden_WhenServiceRejectsAccess()
    {
        using var factory = new StubApiFactory<ICareLogService>(new StubCareLogService
        {
            GetLogByIdException = new DomainException("forbidden", "FORBIDDEN", 403)
        });
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/api/care-groups/aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa/care-logs/bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);

        var payload = await JsonResponseHelper.ReadJsonAsync(response);
        Assert.Equal("FORBIDDEN", payload.RootElement.GetProperty("errorCode").GetString());
    }

    [Fact]
    public async Task UpdateLog_ReturnsConflict_WhenServiceDetectsConcurrencyConflict()
    {
        using var factory = new StubApiFactory<ICareLogService>(new StubCareLogService
        {
            UpdateException = new DomainException("conflict", "CONCURRENCY_CONFLICT", 409)
        });
        using var client = factory.CreateClient();

        var response = await client.PutAsJsonAsync("/api/care-groups/aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa/care-logs/bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb", new
        {
            title = "updated title",
            updatedAt = "2026-03-20T02:00:00Z"
        });

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);

        var payload = await JsonResponseHelper.ReadJsonAsync(response);
        Assert.Equal("CONCURRENCY_CONFLICT", payload.RootElement.GetProperty("errorCode").GetString());
    }

    private sealed class StubCareLogService : ICareLogService
    {
        public Exception? GetLogByIdException { get; init; }
        public Exception? UpdateException { get; init; }

        public Task<SeasonsCare.Api.DTOs.Common.PagedResponse<CareLogResponse>> GetLogsAsync(Guid currentUserId, Guid careGroupId, SeasonsCare.Api.DTOs.Common.PaginationRequest pagination)
        {
            throw new NotImplementedException();
        }

        public Task<CareLogResponse> GetLogByIdAsync(Guid currentUserId, Guid careGroupId, Guid logId)
        {
            if (GetLogByIdException is not null)
            {
                throw GetLogByIdException;
            }

            return Task.FromResult(new CareLogResponse
            {
                Id = logId,
                CareGroupId = careGroupId,
                Title = "ok"
            });
        }

        public Task<CareLogResponse> CreateLogAsync(Guid currentUserId, Guid careGroupId, CreateCareLogRequest request)
        {
            return Task.FromResult(new CareLogResponse
            {
                Id = Guid.NewGuid(),
                CareGroupId = careGroupId,
                Title = request.Title,
                Description = request.Description,
                StartsAt = request.StartsAt ?? DateTime.UtcNow,
                RepeatPattern = request.RepeatPattern,
                Participants = request.Participants ?? new List<string>(),
                Status = request.Status,
                IsImportant = request.IsImportant
            });
        }

        public Task<CareLogResponse> UpdateLogAsync(Guid currentUserId, Guid careGroupId, Guid logId, UpdateCareLogRequest request)
        {
            if (UpdateException is not null)
            {
                throw UpdateException;
            }

            return Task.FromResult(new CareLogResponse
            {
                Id = logId,
                CareGroupId = careGroupId,
                Title = request.Title,
                Description = request.Description,
                StartsAt = request.StartsAt ?? DateTime.UtcNow,
                RepeatPattern = request.RepeatPattern,
                Participants = request.Participants ?? new List<string>(),
                Status = request.Status,
                IsImportant = request.IsImportant,
                UpdatedAt = request.UpdatedAt
            });
        }

        public Task DeleteLogAsync(Guid currentUserId, Guid careGroupId, Guid logId)
        {
            return Task.CompletedTask;
        }
    }
}
