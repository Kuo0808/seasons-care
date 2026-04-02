using System.Net;
using System.Net.Http.Json;
using SeasonsCare.Api.DTOs.HealthRecords.Temperatures;
using SeasonsCare.Api.Exceptions;
using SeasonsCare.Api.Services.HealthRecords;
using SeasonsCare.Api.Tests.Shared;
using SeasonsCare.Api.Tests.Shared.Http;

namespace SeasonsCare.Api.Tests.Integration.HealthRecords;

public class TemperaturesControllerIntegrationTests
{
    [Fact]
    public async Task CreateRecord_ReturnsBadRequest_WhenValueIsInvalid()
    {
        using var factory = new StubApiFactory<ITemperatureService>(new StubTemperatureService());
        using var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/care-groups/aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa/health-records/temperatures", new
        {
            value = 50
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        using var payload = await JsonResponseHelper.ReadJsonAsync(response);
        Assert.Equal("VALIDATION_FAILED", payload.RootElement.GetProperty("errorCode").GetString());
    }

    [Fact]
    public async Task UpdateRecord_ReturnsConflict_WhenServiceDetectsConcurrencyConflict()
    {
        using var factory = new StubApiFactory<ITemperatureService>(new StubTemperatureService
        {
            UpdateException = new DomainException("conflict", "CONCURRENCY_CONFLICT", 409)
        });
        using var client = factory.CreateClient();

        var response = await client.PutAsJsonAsync("/api/care-groups/aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa/health-records/temperatures/bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb", new
        {
            value = 36.9,
            updatedAt = "2026-03-20T02:00:00Z"
        });

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);

        using var payload = await JsonResponseHelper.ReadJsonAsync(response);
        Assert.Equal("CONCURRENCY_CONFLICT", payload.RootElement.GetProperty("errorCode").GetString());
    }

    private sealed class StubTemperatureService : ITemperatureService
    {
        public Exception? UpdateException { get; init; }

        public Task<SeasonsCare.Api.DTOs.Common.PagedResponse<TemperatureResponse>> GetRecordsAsync(Guid currentUserId, Guid careGroupId, SeasonsCare.Api.DTOs.Common.PaginationRequest paginationRequest) => throw new NotImplementedException();
        public Task<TemperatureResponse> GetRecordByIdAsync(Guid currentUserId, Guid careGroupId, Guid recordId) => throw new NotImplementedException();

        public Task<TemperatureResponse> CreateRecordAsync(Guid currentUserId, Guid careGroupId, CreateTemperatureRequest request)
        {
            return Task.FromResult(new TemperatureResponse
            {
                Id = Guid.NewGuid(),
                CareGroupId = careGroupId,
                Value = request.Value,
                UpdatedAt = DateTime.UtcNow,
                CreatedAt = DateTime.UtcNow,
                CreatedBy = currentUserId.ToString()
            });
        }

        public Task<TemperatureResponse> UpdateRecordAsync(Guid currentUserId, Guid careGroupId, Guid recordId, UpdateTemperatureRequest request)
        {
            if (UpdateException is not null)
            {
                throw UpdateException;
            }

            return Task.FromResult(new TemperatureResponse
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
