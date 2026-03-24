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
    public class BloodPressureRepository : IBloodPressureRepository
    {
        private readonly DbContext _context;

        public BloodPressureRepository(DbContext context)
        {
            _context = context;
        }

        public async Task<PagedResponse<BloodPressureRecord>> GetPagedAsync(Guid careGroupId, PaginationRequest request)
        {
            var query = _context.Set<BloodPressureRecord>()
                .Where(x => x.CareGroupId == careGroupId)
                .OrderByDescending(x => x.RecordDate);

            var totalCount = await query.CountAsync();
            var items = await query
                .Skip((request.Page - 1) * request.PageSize)
                .Take(request.PageSize)
                .ToListAsync();

            return new PagedResponse<BloodPressureRecord>(items, totalCount, request.Page, request.PageSize);
        }

        public async Task<BloodPressureRecord?> GetByIdAsync(Guid careGroupId, Guid id)
        {
            return await _context.Set<BloodPressureRecord>()
                .FirstOrDefaultAsync(x => x.CareGroupId == careGroupId && x.Id == id);
        }

        public async Task<BloodPressureRecord> AddAsync(BloodPressureRecord record)
        {
            _context.Set<BloodPressureRecord>().Add(record);
            await _context.SaveChangesAsync();
            return record;
        }

        public async Task<BloodPressureRecord> UpdateAsync(BloodPressureRecord record)
        {
            _context.Set<BloodPressureRecord>().Update(record);
            await _context.SaveChangesAsync();
            return record;
        }
    }
}
