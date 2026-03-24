using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using SeasonsCare.Api.DTOs.Common;
using SeasonsCare.Api.DTOs.HealthRecords.Weights;

namespace SeasonsCare.Api.Services.HealthRecords
{
    public interface IWeightService
    {
        Task<PagedResponse<WeightResponse>> GetRecordsAsync(Guid currentUserId, Guid careGroupId, PaginationRequest paginationRequest);
        Task<WeightResponse> GetRecordByIdAsync(Guid currentUserId, Guid careGroupId, Guid recordId);
        Task<WeightResponse> CreateRecordAsync(Guid currentUserId, Guid careGroupId, CreateWeightRequest request);
        Task<WeightResponse> UpdateRecordAsync(Guid currentUserId, Guid careGroupId, Guid recordId, UpdateWeightRequest request);
        Task DeleteRecordAsync(Guid currentUserId, Guid careGroupId, Guid recordId);
    }
}
