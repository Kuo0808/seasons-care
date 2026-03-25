using System.Net;
using System.Net.Http.Json;
using SeasonsCare.Api.DTOs.HealthRecords.BloodOxygens;
using SeasonsCare.Api.Exceptions;
using SeasonsCare.Api.Services.HealthRecords;
using SeasonsCare.Api.Tests.Shared;
using SeasonsCare.Api.Tests.Shared.Http;

namespace SeasonsCare.Api.Tests.Integration.HealthRecords;

public class BloodOxygensControllerIntegrationTests
{
    [Fact]
    public async Task CreateRecord_ReturnsBadRequest_WhenSpO2IsInvalid()
    {
        using var factory = new StubApiFactory<IBloodOxygenService>(new StubBloodOxygenService());
        using var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/care-groups/aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa/health-records/blood-oxygens", new
        {
            spO2 = 101
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        using var payload = await JsonResponseHelper.ReadJsonAsync(response);
        Assert.Equal("VALIDATION_FAILED", payload.RootElement.GetProperty("errorCode").GetString());
    }

    [Fact]
    public async Task UpdateRecord_ReturnsConflict_WhenServiceDetectsConcurrencyConflict()
    {
        using var factory = new StubApiFactory<IBloodOxygenService>(new StubBloodOxygenService
        {
            UpdateException = new DomainException("conflict", "CONCURRENCY_CONFLICT", 409)
        });
        using var client = factory.CreateClient();

        var response = await client.PutAsJsonAsync("/api/care-groups/aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa/health-records/blood-oxygens/bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb", new
        {
            spO2 = 98,
            updatedAt = "2026-03-20T02:00:00Z"
        });

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);

        using var payload = await JsonResponseHelper.ReadJsonAsync(response);
        Assert.Equal("CONCURRENCY_CONFLICT", payload.RootElement.GetProperty("errorCode").GetString());
    }

    private sealed class StubBloodOxygenService : IBloodOxygenService
    {
        public Exception? UpdateException { get; init; }

        public Task<SeasonsCare.Api.DTOs.Common.PagedResponse<BloodOxygenResponse>> GetRecordsAsync(Guid currentUserId, Guid careGroupId, SeasonsCare.Api.DTOs.Common.PaginationRequest paginationRequest) => throw new NotImplementedException();
        public Task<BloodOxygenResponse> GetRecordByIdAsync(Guid currentUserId, Guid careGroupId, Guid recordId) => throw new NotImplementedException();

        public Task<BloodOxygenResponse> CreateRecordAsync(Guid currentUserId, Guid careGroupId, CreateBloodOxygenRequest request)
        {
            return Task.FromResult(new BloodOxygenResponse
            {
                Id = Guid.NewGuid(),
                CareGroupId = careGroupId,
                SpO2 = request.SpO2,
                UpdatedAt = DateTime.UtcNow,
                CreatedAt = DateTime.UtcNow,
                CreatedBy = currentUserId.ToString()
            });
        }

        public Task<BloodOxygenResponse> UpdateRecordAsync(Guid currentUserId, Guid careGroupId, Guid recordId, UpdateBloodOxygenRequest request)
        {
            if (UpdateException is not null)
            {
                throw UpdateException;
            }

            return Task.FromResult(new BloodOxygenResponse
            {
                Id = recordId,
                CareGroupId = careGroupId,
                SpO2 = request.SpO2,
                UpdatedAt = request.UpdatedAt,
                CreatedAt = DateTime.UtcNow,
                CreatedBy = currentUserId.ToString()
            });
        }

        public Task DeleteRecordAsync(Guid currentUserId, Guid careGroupId, Guid recordId) => Task.CompletedTask;
    }
}
