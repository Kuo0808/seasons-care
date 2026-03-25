using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using SeasonsCare.Api.Models.Entities.HealthRecords;
using SeasonsCare.Api.DTOs.Common;

namespace SeasonsCare.Api.Repositories.HealthRecords
{
    public interface IBloodOxygenRepository
    {
        Task<PagedResponse<BloodOxygenRecord>> GetPagedAsync(Guid careGroupId, PaginationRequest request);
        Task<BloodOxygenRecord?> GetByIdAsync(Guid careGroupId, Guid id);
        Task<BloodOxygenRecord> AddAsync(BloodOxygenRecord record);
        Task<BloodOxygenRecord> UpdateAsync(BloodOxygenRecord record);
    }
}
