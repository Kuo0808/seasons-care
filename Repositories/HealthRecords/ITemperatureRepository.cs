using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using SeasonsCare.Api.Models.Entities.HealthRecords;
using SeasonsCare.Api.DTOs.Common;

namespace SeasonsCare.Api.Repositories.HealthRecords
{
    public interface ITemperatureRepository
    {
        Task<PagedResponse<TemperatureRecord>> GetPagedAsync(Guid careGroupId, PaginationRequest request);
        Task<TemperatureRecord?> GetByIdAsync(Guid careGroupId, Guid id);
        Task<TemperatureRecord> AddAsync(TemperatureRecord record);
        Task<TemperatureRecord> UpdateAsync(TemperatureRecord record);
    }
}
