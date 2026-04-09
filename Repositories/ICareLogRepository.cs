using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using SeasonsCare.Api.DTOs.Common;
using SeasonsCare.Api.Models.Entities;

namespace SeasonsCare.Api.Repositories
{
    public interface ICareLogRepository
    {
        Task<CareLog?> GetByIdAsync(Guid id);
        Task<(List<CareLog> Data, int TotalCount)> GetPagedByCareGroupIdAsync(Guid careGroupId, int page, int pageSize, string sort);
        Task<(List<CareLog> Data, int TotalCount)> GetPagedByCareGroupIdAsync(Guid careGroupId, PaginationRequest request);
        Task AddAsync(CareLog careLog);
        Task UpdateAsync(CareLog careLog);
        Task SaveChangesAsync();
    }
}
