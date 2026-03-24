using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using SeasonsCare.Api.Data;
using SeasonsCare.Api.Models.Entities.HealthRecords;
using SeasonsCare.Api.DTOs.Common;

namespace SeasonsCare.Api.Repositories.HealthRecords
{
    public class TemperatureRepository : ITemperatureRepository
    {
        private readonly DbContext _context;

        public TemperatureRepository(DbContext context)
        {
            _context = context;
        }

        public async Task<PagedResponse<TemperatureRecord>> GetPagedAsync(Guid careGroupId, PaginationRequest request)
        {
            var query = _context.Set<TemperatureRecord>()
                .Where(x => x.CareGroupId == careGroupId)
                .OrderByDescending(x => x.RecordDate);

            var totalCount = await query.CountAsync();
            var items = await query
                .Skip((request.Page - 1) * request.PageSize)
                .Take(request.PageSize)
                .ToListAsync();

            return new PagedResponse<TemperatureRecord>(items, totalCount, request.Page, request.PageSize);
        }

        public async Task<TemperatureRecord?> GetByIdAsync(Guid careGroupId, Guid id)
        {
            return await _context.Set<TemperatureRecord>()
                .FirstOrDefaultAsync(x => x.CareGroupId == careGroupId && x.Id == id);
        }

        public async Task<TemperatureRecord> AddAsync(TemperatureRecord record)
        {
            _context.Set<TemperatureRecord>().Add(record);
            await _context.SaveChangesAsync();
            return record;
        }

        public async Task<TemperatureRecord> UpdateAsync(TemperatureRecord record)
        {
            _context.Set<TemperatureRecord>().Update(record);
            await _context.SaveChangesAsync();
            return record;
        }
    }
}
