using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using SeasonsCare.Api.Data;
using SeasonsCare.Api.Models.Entities;

namespace SeasonsCare.Api.Repositories
{
    public class EventSeriesRepository : IEventSeriesRepository
    {
        private readonly ApplicationDbContext _context;

        public EventSeriesRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<EventSeries?> GetByIdAsync(Guid id)
        {
            return await _context.EventSeries.FirstOrDefaultAsync(x => x.Id == id);
        }

        public async Task<List<EventSeries>> GetAllByCareGroupIdAsync(Guid careGroupId)
        {
            return await _context.EventSeries
                .Where(x => x.CareGroupId == careGroupId)
                .OrderBy(x => x.StartsAt)
                .ToListAsync();
        }

        public async Task<(List<EventSeries> Data, int TotalCount)> GetPagedByCareGroupIdAsync(Guid careGroupId, int page, int pageSize, string sort)
        {
            var query = _context.EventSeries.Where(x => x.CareGroupId == careGroupId);
            var totalCount = await query.CountAsync();

            query = sort switch
            {
                "startsAt_asc" => query.OrderBy(x => x.StartsAt),
                "startsAt_desc" => query.OrderByDescending(x => x.StartsAt),
                _ => query.OrderByDescending(x => x.CreatedAt)
            };

            var data = await query.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();
            return (data, totalCount);
        }

        public async Task AddAsync(EventSeries series)
        {
            await _context.EventSeries.AddAsync(series);
        }

        public async Task SaveChangesAsync()
        {
            await _context.SaveChangesAsync();
        }
    }
}
