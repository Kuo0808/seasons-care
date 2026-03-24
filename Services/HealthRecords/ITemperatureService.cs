using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using SeasonsCare.Api.DTOs.Common;
using SeasonsCare.Api.DTOs.HealthRecords.Temperatures;

namespace SeasonsCare.Api.Services.HealthRecords
{
    public interface ITemperatureService
    {
        Task<PagedResponse<TemperatureResponse>> GetRecordsAsync(Guid currentUserId, Guid careGroupId, PaginationRequest paginationRequest);
        Task<TemperatureResponse> GetRecordByIdAsync(Guid currentUserId, Guid careGroupId, Guid recordId);
        Task<TemperatureResponse> CreateRecordAsync(Guid currentUserId, Guid careGroupId, CreateTemperatureRequest request);
        Task<TemperatureResponse> UpdateRecordAsync(Guid currentUserId, Guid careGroupId, Guid recordId, UpdateTemperatureRequest request);
        Task DeleteRecordAsync(Guid currentUserId, Guid careGroupId, Guid recordId);
    }
}
