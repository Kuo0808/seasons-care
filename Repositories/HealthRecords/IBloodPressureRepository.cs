using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using SeasonsCare.Api.Models.Entities.HealthRecords;
using SeasonsCare.Api.DTOs.Common;

namespace SeasonsCare.Api.Repositories.HealthRecords
{
    public interface IBloodPressureRepository
    {
        Task<PagedResponse<BloodPressureRecord>> GetPagedAsync(Guid careGroupId, PaginationRequest request);
        Task<List<BloodPressureRecord>> GetByCareGroupIdAndDateRangeAsync(Guid careGroupId, DateTime dateFrom, DateTime dateTo);
        Task<BloodPressureRecord?> GetByIdAsync(Guid careGroupId, Guid id);
        Task<BloodPressureRecord> AddAsync(BloodPressureRecord record);
        Task<BloodPressureRecord> UpdateAsync(BloodPressureRecord record);
    }
}
