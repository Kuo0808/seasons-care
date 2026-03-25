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
    public class BloodOxygenRepository : IBloodOxygenRepository
    {
        private readonly DbContext _context;

        public BloodOxygenRepository(DbContext context)
        {
            _context = context;
        }

        public async Task<PagedResponse<BloodOxygenRecord>> GetPagedAsync(Guid careGroupId, PaginationRequest request)
        {
            var query = _context.Set<BloodOxygenRecord>()
                .Where(x => x.CareGroupId == careGroupId)
                .OrderByDescending(x => x.RecordDate);

            var totalCount = await query.CountAsync();
            var items = await query
                .Skip((request.Page - 1) * request.PageSize)
                .Take(request.PageSize)
                .ToListAsync();

            return new PagedResponse<BloodOxygenRecord>(items, totalCount, request.Page, request.PageSize);
        }

        public async Task<BloodOxygenRecord?> GetByIdAsync(Guid careGroupId, Guid id)
        {
            return await _context.Set<BloodOxygenRecord>()
                .FirstOrDefaultAsync(x => x.CareGroupId == careGroupId && x.Id == id);
        }

        public async Task<BloodOxygenRecord> AddAsync(BloodOxygenRecord record)
        {
            _context.Set<BloodOxygenRecord>().Add(record);
            await _context.SaveChangesAsync();
            return record;
        }

        public async Task<BloodOxygenRecord> UpdateAsync(BloodOxygenRecord record)
        {
            _context.Set<BloodOxygenRecord>().Update(record);
            await _context.SaveChangesAsync();
            return record;
        }
    }
}
