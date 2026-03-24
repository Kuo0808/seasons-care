using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using SeasonsCare.Api.Models.Entities.HealthRecords;
using SeasonsCare.Api.DTOs.Common;

namespace SeasonsCare.Api.Repositories.HealthRecords
{
    public interface IWeightRepository
    {
        Task<PagedResponse<WeightRecord>> GetPagedAsync(Guid careGroupId, PaginationRequest request);
        Task<WeightRecord?> GetByIdAsync(Guid careGroupId, Guid id);
        Task<WeightRecord> AddAsync(WeightRecord record);
        Task<WeightRecord> UpdateAsync(WeightRecord record);
    }
}
