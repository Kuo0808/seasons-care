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
    public class WeightRepository : IWeightRepository
    {
        private readonly DbContext _context;

        public WeightRepository(DbContext context)
        {
            _context = context;
        }

        public async Task<PagedResponse<WeightRecord>> GetPagedAsync(Guid careGroupId, PaginationRequest request)
        {
            var query = _context.Set<WeightRecord>()
                .Where(x => x.CareGroupId == careGroupId)
                .OrderByDescending(x => x.RecordDate);

            var totalCount = await query.CountAsync();
            var items = await query
                .Skip((request.Page - 1) * request.PageSize)
                .Take(request.PageSize)
                .ToListAsync();

            return new PagedResponse<WeightRecord>(items, totalCount, request.Page, request.PageSize);
        }

        public async Task<WeightRecord?> GetByIdAsync(Guid careGroupId, Guid id)
        {
            return await _context.Set<WeightRecord>()
                .FirstOrDefaultAsync(x => x.CareGroupId == careGroupId && x.Id == id);
        }

        public async Task<WeightRecord> AddAsync(WeightRecord record)
        {
            _context.Set<WeightRecord>().Add(record);
            await _context.SaveChangesAsync();
            return record;
        }

        public async Task<WeightRecord> UpdateAsync(WeightRecord record)
        {
            _context.Set<WeightRecord>().Update(record);
            await _context.SaveChangesAsync();
            return record;
        }
    }
}
