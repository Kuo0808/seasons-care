using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using SeasonsCare.Api.Models.Entities.HealthRecords;
using SeasonsCare.Api.DTOs.Common;

namespace SeasonsCare.Api.Repositories.HealthRecords
{
    public interface IBloodSugarRepository
    {
        Task<PagedResponse<BloodSugarRecord>> GetPagedAsync(Guid careGroupId, PaginationRequest request);
        Task<List<BloodSugarRecord>> GetByCareGroupIdAndDateRangeAsync(Guid careGroupId, DateTime dateFrom, DateTime dateTo);
        Task<BloodSugarRecord?> GetByIdAsync(Guid careGroupId, Guid id);
        Task<BloodSugarRecord> AddAsync(BloodSugarRecord record);
        Task<BloodSugarRecord> UpdateAsync(BloodSugarRecord record);
    }
}
