using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using SeasonsCare.Api.Models.Entities;

namespace SeasonsCare.Api.Repositories
{
    public interface IEventOccurrenceRepository
    {
        Task<List<EventOccurrence>> GetByRangeAsync(Guid careGroupId, DateTime from, DateTime to);
        Task<EventOccurrence?> GetBySeriesIdAndScheduledStartAtAsync(Guid eventSeriesId, DateTime scheduledStartAt);
        Task AddAsync(EventOccurrence occurrence);
        Task UpdateAsync(EventOccurrence occurrence);
        Task SaveChangesAsync();
    }
}
