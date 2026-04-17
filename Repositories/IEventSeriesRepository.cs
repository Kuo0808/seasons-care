using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using SeasonsCare.Api.Models.Entities;

namespace SeasonsCare.Api.Repositories
{
    public interface IEventSeriesRepository
    {
        Task<EventSeries?> GetByIdAsync(Guid id);
        Task<List<EventSeries>> GetAllByCareGroupIdAsync(Guid careGroupId);
        Task<(List<EventSeries> Data, int TotalCount)> GetPagedByCareGroupIdAsync(Guid careGroupId, int page, int pageSize, string sort);
        Task AddAsync(EventSeries series);
        Task UpdateAsync(EventSeries series);
        Task SoftDeleteAsync(EventSeries series);
        Task SaveChangesAsync();
    }
}
