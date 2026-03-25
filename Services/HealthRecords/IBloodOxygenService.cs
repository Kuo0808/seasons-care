using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using SeasonsCare.Api.DTOs.Common;
using SeasonsCare.Api.DTOs.HealthRecords.BloodOxygens;

namespace SeasonsCare.Api.Services.HealthRecords
{
    public interface IBloodOxygenService
    {
        Task<PagedResponse<BloodOxygenResponse>> GetRecordsAsync(Guid currentUserId, Guid careGroupId, PaginationRequest paginationRequest);
        Task<BloodOxygenResponse> GetRecordByIdAsync(Guid currentUserId, Guid careGroupId, Guid recordId);
        Task<BloodOxygenResponse> CreateRecordAsync(Guid currentUserId, Guid careGroupId, CreateBloodOxygenRequest request);
        Task<BloodOxygenResponse> UpdateRecordAsync(Guid currentUserId, Guid careGroupId, Guid recordId, UpdateBloodOxygenRequest request);
        Task DeleteRecordAsync(Guid currentUserId, Guid careGroupId, Guid recordId);
    }
}
