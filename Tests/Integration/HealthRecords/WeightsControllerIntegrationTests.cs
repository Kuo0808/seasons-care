using System.Net;
using System.Net.Http.Json;
using SeasonsCare.Api.DTOs.HealthRecords.Weights;
using SeasonsCare.Api.Exceptions;
using SeasonsCare.Api.Services.HealthRecords;
using SeasonsCare.Api.Tests.Shared;
using SeasonsCare.Api.Tests.Shared.Http;

namespace SeasonsCare.Api.Tests.Integration.HealthRecords;

public class WeightsControllerIntegrationTests
{
    [Fact]
    public async Task CreateRecord_ReturnsBadRequest_WhenValueIsInvalid()
    {
        using var factory = new StubApiFactory<IWeightService>(new StubWeightService());
        using var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/care-groups/aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa/health-records/weights", new
        {
            value = 0
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        using var payload = await JsonResponseHelper.ReadJsonAsync(response);
        Assert.Equal("VALIDATION_FAILED", payload.RootElement.GetProperty("errorCode").GetString());
    }

    [Fact]
    public async Task UpdateRecord_ReturnsConflict_WhenServiceDetectsConcurrencyConflict()
    {
        using var factory = new StubApiFactory<IWeightService>(new StubWeightService
        {
            UpdateException = new DomainException("conflict", "CONCURRENCY_CONFLICT", 409)
        });
        using var client = factory.CreateClient();

        var response = await client.PutAsJsonAsync("/api/care-groups/aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa/health-records/weights/bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb", new
        {
            value = 61.2,
            updatedAt = "2026-03-20T02:00:00Z"
        });

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);

        using var payload = await JsonResponseHelper.ReadJsonAsync(response);
        Assert.Equal("CONCURRENCY_CONFLICT", payload.RootElement.GetProperty("errorCode").GetString());
    }

    private sealed class StubWeightService : IWeightService
    {
        public Exception? UpdateException { get; init; }

        public Task<SeasonsCare.Api.DTOs.Common.PagedResponse<WeightResponse>> GetRecordsAsync(Guid currentUserId, Guid careGroupId, SeasonsCare.Api.DTOs.Common.PaginationRequest paginationRequest) => throw new NotImplementedException();
        public Task<WeightResponse> GetRecordByIdAsync(Guid currentUserId, Guid careGroupId, Guid recordId) => throw new NotImplementedException();

        public Task<WeightResponse> CreateRecordAsync(Guid currentUserId, Guid careGroupId, CreateWeightRequest request)
        {
            return Task.FromResult(new WeightResponse
            {
                Id = Guid.NewGuid(),
                CareGroupId = careGroupId,
                Value = request.Value,
                UpdatedAt = DateTime.UtcNow,
                CreatedAt = DateTime.UtcNow,
                CreatedBy = currentUserId.ToString()
            });
        }

        public Task<WeightResponse> UpdateRecordAsync(Guid currentUserId, Guid careGroupId, Guid recordId, UpdateWeightRequest request)
        {
            if (UpdateException is not null)
            {
                throw UpdateException;
            }

            return Task.FromResult(new WeightResponse
            {
                Id = recordId,
                CareGroupId = careGroupId,
                Value = request.Value,
                UpdatedAt = request.UpdatedAt,
                CreatedAt = DateTime.UtcNow,
                CreatedBy = currentUserId.ToString()
            });
        }

        public Task DeleteRecordAsync(Guid currentUserId, Guid careGroupId, Guid recordId) => Task.CompletedTask;
    }
}
