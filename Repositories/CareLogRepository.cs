using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using SeasonsCare.Api.Data;
using SeasonsCare.Api.Models.Entities;

namespace SeasonsCare.Api.Repositories
{
    public class CareLogRepository : ICareLogRepository
    {
        private readonly ApplicationDbContext _context;

        public CareLogRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<CareLog?> GetByIdAsync(Guid id)
        {
            return await _context.CareLogs.FirstOrDefaultAsync(l => l.Id == id);
        }

        public async Task<(List<CareLog> Data, int TotalCount)> GetPagedByCareGroupIdAsync(Guid careGroupId, int page, int pageSize, string sort)
        {
            // _context.CurrentCareGroupId should be set by middleware, but we can explicitly filter just in case
            var query = _context.CareLogs.Where(l => l.CareGroupId == careGroupId);

            var totalCount = await query.CountAsync();

            query = sort switch
            {
                "createdAt_asc" => query.OrderBy(l => l.CreatedAt),
                "recordDate_desc" => query.OrderByDescending(l => l.RecordDate),
                "recordDate_asc" => query.OrderBy(l => l.RecordDate),
                _ => query.OrderByDescending(l => l.CreatedAt) // default to createdAt_desc per rules
            };

            var data = await query.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();

            return (data, totalCount);
        }

        public async Task AddAsync(CareLog careLog)
        {
            await _context.CareLogs.AddAsync(careLog);
        }

        public async Task UpdateAsync(CareLog careLog)
        {
            _context.CareLogs.Update(careLog);
            await Task.CompletedTask;
        }

        public async Task SaveChangesAsync()
        {
            await _context.SaveChangesAsync();
        }
    }
}
