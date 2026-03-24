using System.Net;
using System.Net.Http.Json;
using SeasonsCare.Api.DTOs.HealthRecords.BloodPressures;
using SeasonsCare.Api.Exceptions;
using SeasonsCare.Api.Services.HealthRecords;
using SeasonsCare.Api.Tests.Shared;
using SeasonsCare.Api.Tests.Shared.Http;

namespace SeasonsCare.Api.Tests.Integration.HealthRecords;

public class BloodPressuresControllerIntegrationTests
{
    [Fact]
    public async Task CreateRecord_ReturnsBadRequest_WhenSystolicIsInvalid()
    {
        using var factory = new StubApiFactory<IBloodPressureService>(new StubBloodPressureService());
        using var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/care-groups/aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa/health-records/blood-pressures", new
        {
            systolic = 0,
            diastolic = 80
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        using var payload = await JsonResponseHelper.ReadJsonAsync(response);
        Assert.Equal("VALIDATION_FAILED", payload.RootElement.GetProperty("errorCode").GetString());
    }

    [Fact]
    public async Task UpdateRecord_ReturnsConflict_WhenServiceDetectsConcurrencyConflict()
    {
        using var factory = new StubApiFactory<IBloodPressureService>(new StubBloodPressureService
        {
            UpdateException = new DomainException("conflict", "CONCURRENCY_CONFLICT", 409)
        });
        using var client = factory.CreateClient();

        var response = await client.PutAsJsonAsync("/api/care-groups/aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa/health-records/blood-pressures/bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb", new
        {
            systolic = 125,
            diastolic = 82,
            updatedAt = "2026-03-20T02:00:00Z"
        });

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);

        using var payload = await JsonResponseHelper.ReadJsonAsync(response);
        Assert.Equal("CONCURRENCY_CONFLICT", payload.RootElement.GetProperty("errorCode").GetString());
    }

    private sealed class StubBloodPressureService : IBloodPressureService
    {
        public Exception? UpdateException { get; init; }

        public Task<SeasonsCare.Api.DTOs.Common.PagedResponse<BloodPressureResponse>> GetRecordsAsync(Guid currentUserId, Guid careGroupId, SeasonsCare.Api.DTOs.Common.PaginationRequest paginationRequest) => throw new NotImplementedException();
        public Task<BloodPressureResponse> GetRecordByIdAsync(Guid currentUserId, Guid careGroupId, Guid recordId) => throw new NotImplementedException();

        public Task<BloodPressureResponse> CreateRecordAsync(Guid currentUserId, Guid careGroupId, CreateBloodPressureRequest request)
        {
            return Task.FromResult(new BloodPressureResponse
            {
                Id = Guid.NewGuid(),
                CareGroupId = careGroupId,
                Systolic = request.Systolic,
                Diastolic = request.Diastolic,
                UpdatedAt = DateTime.UtcNow,
                CreatedAt = DateTime.UtcNow,
                CreatedBy = currentUserId.ToString()
            });
        }

        public Task<BloodPressureResponse> UpdateRecordAsync(Guid currentUserId, Guid careGroupId, Guid recordId, UpdateBloodPressureRequest request)
        {
            if (UpdateException is not null)
            {
                throw UpdateException;
            }

            return Task.FromResult(new BloodPressureResponse
            {
                Id = recordId,
                CareGroupId = careGroupId,
                Systolic = request.Systolic,
                Diastolic = request.Diastolic,
                UpdatedAt = request.UpdatedAt,
                CreatedAt = DateTime.UtcNow,
                CreatedBy = currentUserId.ToString()
            });
        }

        public Task DeleteRecordAsync(Guid currentUserId, Guid careGroupId, Guid recordId) => Task.CompletedTask;
    }
}
