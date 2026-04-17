using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using SeasonsCare.Api.Models.Entities;

namespace SeasonsCare.Api.Repositories
{
    public interface IEventOccurrenceRepository
    {
        Task<List<EventOccurrence>> GetByRangeAsync(Guid careGroupId, DateTime from, DateTime to);
        Task<EventOccurrence?> GetBySeriesIdAndOccurrenceKeyStartAtAsync(Guid eventSeriesId, DateTime occurrenceKeyStartAt);
        Task<EventOccurrence?> GetBySeriesIdAndEffectiveStartAtAsync(Guid eventSeriesId, DateTime effectiveStartAt);
        Task AddAsync(EventOccurrence occurrence);
        Task UpdateAsync(EventOccurrence occurrence);
        Task SaveChangesAsync();
    }
}
