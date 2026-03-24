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
    public class BloodSugarRepository : IBloodSugarRepository
    {
        private readonly DbContext _context;

        public BloodSugarRepository(DbContext context)
        {
            _context = context;
        }

        public async Task<PagedResponse<BloodSugarRecord>> GetPagedAsync(Guid careGroupId, PaginationRequest request)
        {
            var query = _context.Set<BloodSugarRecord>()
                .Where(x => x.CareGroupId == careGroupId)
                .OrderByDescending(x => x.RecordDate);

            var totalCount = await query.CountAsync();
            var items = await query
                .Skip((request.Page - 1) * request.PageSize)
                .Take(request.PageSize)
                .ToListAsync();

            return new PagedResponse<BloodSugarRecord>(items, totalCount, request.Page, request.PageSize);
        }

        public async Task<BloodSugarRecord?> GetByIdAsync(Guid careGroupId, Guid id)
        {
            return await _context.Set<BloodSugarRecord>()
                .FirstOrDefaultAsync(x => x.CareGroupId == careGroupId && x.Id == id);
        }

        public async Task<BloodSugarRecord> AddAsync(BloodSugarRecord record)
        {
            _context.Set<BloodSugarRecord>().Add(record);
            await _context.SaveChangesAsync();
            return record;
        }

        public async Task<BloodSugarRecord> UpdateAsync(BloodSugarRecord record)
        {
            _context.Set<BloodSugarRecord>().Update(record);
            await _context.SaveChangesAsync();
            return record;
        }
    }
}
