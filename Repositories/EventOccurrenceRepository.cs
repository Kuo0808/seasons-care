using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using SeasonsCare.Api.Data;
using SeasonsCare.Api.Models.Entities;

namespace SeasonsCare.Api.Repositories
{
    public class EventOccurrenceRepository : IEventOccurrenceRepository
    {
        private readonly ApplicationDbContext _context;

        public EventOccurrenceRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<List<EventOccurrence>> GetByRangeAsync(Guid careGroupId, DateTime from, DateTime to)
        {
            return await _context.EventOccurrences
                .Where(x => x.CareGroupId == careGroupId &&
                            (x.OverrideStartAt ?? x.OccurrenceKeyStartAt) >= from &&
                            (x.OverrideStartAt ?? x.OccurrenceKeyStartAt) <= to)
                .ToListAsync();
        }

        public async Task<EventOccurrence?> GetBySeriesIdAndOccurrenceKeyStartAtAsync(Guid eventSeriesId, DateTime occurrenceKeyStartAt)
        {
            return await _context.EventOccurrences
                .FirstOrDefaultAsync(x => x.EventSeriesId == eventSeriesId && x.OccurrenceKeyStartAt == occurrenceKeyStartAt);
        }

        public async Task<EventOccurrence?> GetBySeriesIdAndEffectiveStartAtAsync(Guid eventSeriesId, DateTime effectiveStartAt)
        {
            return await _context.EventOccurrences
                .FirstOrDefaultAsync(x => x.EventSeriesId == eventSeriesId &&
                                          (x.OverrideStartAt ?? x.OccurrenceKeyStartAt) == effectiveStartAt);
        }

        public async Task AddAsync(EventOccurrence occurrence)
        {
            await _context.EventOccurrences.AddAsync(occurrence);
        }

        public async Task UpdateAsync(EventOccurrence occurrence)
        {
            _context.EventOccurrences.Update(occurrence);
            await Task.CompletedTask;
        }

        public async Task SaveChangesAsync()
        {
            await _context.SaveChangesAsync();
        }
    }
}
