using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using SeasonsCare.Api.DTOs.Common;
using SeasonsCare.Api.DTOs.HealthRecords.BloodSugars;

namespace SeasonsCare.Api.Services.HealthRecords
{
    public interface IBloodSugarService
    {
        Task<PagedResponse<BloodSugarResponse>> GetRecordsAsync(Guid currentUserId, Guid careGroupId, PaginationRequest paginationRequest);
        Task<BloodSugarResponse> GetRecordByIdAsync(Guid currentUserId, Guid careGroupId, Guid recordId);
        Task<BloodSugarResponse> CreateRecordAsync(Guid currentUserId, Guid careGroupId, CreateBloodSugarRequest request);
        Task<BloodSugarResponse> UpdateRecordAsync(Guid currentUserId, Guid careGroupId, Guid recordId, UpdateBloodSugarRequest request);
        Task DeleteRecordAsync(Guid currentUserId, Guid careGroupId, Guid recordId);
    }
}
