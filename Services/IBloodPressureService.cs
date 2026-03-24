using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using SeasonsCare.Api.DTOs.Common;
using SeasonsCare.Api.DTOs.BloodPressures;

namespace SeasonsCare.Api.Services
{
    public interface IBloodPressureService
    {
        Task<PagedResponse<BloodPressureResponse>> GetRecordsAsync(Guid currentUserId, Guid careGroupId, PaginationRequest paginationRequest);
        Task<BloodPressureResponse> GetRecordByIdAsync(Guid currentUserId, Guid careGroupId, Guid recordId);
        Task<BloodPressureResponse> CreateRecordAsync(Guid currentUserId, Guid careGroupId, CreateBloodPressureRequest request);
        Task<BloodPressureResponse> UpdateRecordAsync(Guid currentUserId, Guid careGroupId, Guid recordId, UpdateBloodPressureRequest request);
        Task DeleteRecordAsync(Guid currentUserId, Guid careGroupId, Guid recordId);
    }
}
