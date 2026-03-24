using System.Net;
using System.Net.Http.Json;
using SeasonsCare.Api.DTOs.HealthRecords.BloodSugars;
using SeasonsCare.Api.Exceptions;
using SeasonsCare.Api.Services.HealthRecords;
using SeasonsCare.Api.Tests.Shared;
using SeasonsCare.Api.Tests.Shared.Http;

namespace SeasonsCare.Api.Tests.Integration.HealthRecords;

public class BloodSugarsControllerIntegrationTests
{
    [Fact]
    public async Task CreateRecord_ReturnsBadRequest_WhenMeasurementContextIsMissing()
    {
        using var factory = new StubApiFactory<IBloodSugarService>(new StubBloodSugarService());
        using var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/care-groups/aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa/health-records/blood-sugars", new
        {
            glucoseLevel = 120,
            measurementContext = ""
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        using var payload = await JsonResponseHelper.ReadJsonAsync(response);
        Assert.Equal("VALIDATION_FAILED", payload.RootElement.GetProperty("errorCode").GetString());
    }

    [Fact]
    public async Task UpdateRecord_ReturnsConflict_WhenServiceDetectsConcurrencyConflict()
    {
        using var factory = new StubApiFactory<IBloodSugarService>(new StubBloodSugarService
        {
            UpdateException = new DomainException("conflict", "CONCURRENCY_CONFLICT", 409)
        });
        using var client = factory.CreateClient();

        var response = await client.PutAsJsonAsync("/api/care-groups/aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa/health-records/blood-sugars/bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb", new
        {
            glucoseLevel = 125,
            measurementContext = "飯後",
            updatedAt = "2026-03-20T02:00:00Z"
        });

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);

        using var payload = await JsonResponseHelper.ReadJsonAsync(response);
        Assert.Equal("CONCURRENCY_CONFLICT", payload.RootElement.GetProperty("errorCode").GetString());
    }

    private sealed class StubBloodSugarService : IBloodSugarService
    {
        public Exception? UpdateException { get; init; }

        public Task<SeasonsCare.Api.DTOs.Common.PagedResponse<BloodSugarResponse>> GetRecordsAsync(Guid currentUserId, Guid careGroupId, SeasonsCare.Api.DTOs.Common.PaginationRequest paginationRequest) => throw new NotImplementedException();
        public Task<BloodSugarResponse> GetRecordByIdAsync(Guid currentUserId, Guid careGroupId, Guid recordId) => throw new NotImplementedException();

        public Task<BloodSugarResponse> CreateRecordAsync(Guid currentUserId, Guid careGroupId, CreateBloodSugarRequest request)
        {
            return Task.FromResult(new BloodSugarResponse
            {
                Id = Guid.NewGuid(),
                CareGroupId = careGroupId,
                GlucoseLevel = request.GlucoseLevel,
                MeasurementContext = request.MeasurementContext,
                UpdatedAt = DateTime.UtcNow,
                CreatedAt = DateTime.UtcNow,
                CreatedBy = currentUserId.ToString()
            });
        }

        public Task<BloodSugarResponse> UpdateRecordAsync(Guid currentUserId, Guid careGroupId, Guid recordId, UpdateBloodSugarRequest request)
        {
            if (UpdateException is not null)
            {
                throw UpdateException;
            }

            return Task.FromResult(new BloodSugarResponse
            {
                Id = recordId,
                CareGroupId = careGroupId,
                GlucoseLevel = request.GlucoseLevel,
                MeasurementContext = request.MeasurementContext,
                UpdatedAt = request.UpdatedAt,
                CreatedAt = DateTime.UtcNow,
                CreatedBy = currentUserId.ToString()
            });
        }

        public Task DeleteRecordAsync(Guid currentUserId, Guid careGroupId, Guid recordId) => Task.CompletedTask;
    }
}
