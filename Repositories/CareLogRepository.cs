using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using SeasonsCare.Api.Data;
using SeasonsCare.Api.DTOs.Common;
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
            var query = _context.CareLogs.Where(l => l.CareGroupId == careGroupId);

            var totalCount = await query.CountAsync();

            query = sort switch
            {
                "createdAt_asc" => query.OrderBy(l => l.CreatedAt),
                "startsAt_desc" => query.OrderByDescending(l => l.StartsAt),
                "startsAt_asc" => query.OrderBy(l => l.StartsAt),
                "recordDate_desc" => query.OrderByDescending(l => l.StartsAt),
                "recordDate_asc" => query.OrderBy(l => l.StartsAt),
                _ => query.OrderByDescending(l => l.CreatedAt)
            };

            var data = await query.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();

            return (data, totalCount);
        }

        public async Task<(List<CareLog> Data, int TotalCount)> GetPagedByCareGroupIdAsync(Guid careGroupId, PaginationRequest request)
        {
            var query = _context.CareLogs
                .Where(l => l.CareGroupId == careGroupId);

            query = ApplyCareLogDateRange(query, request);

            var totalCount = await query.CountAsync();

            query = request.Sort switch
            {
                "createdAt_asc" => query.OrderBy(l => l.CreatedAt),
                "createdAt_desc" => query.OrderByDescending(l => l.CreatedAt),
                "startsAt_asc" => query.OrderBy(l => l.StartsAt),
                "startsAt_desc" => query.OrderByDescending(l => l.StartsAt),
                "recordDate_asc" => query.OrderBy(l => l.StartsAt),
                "recordDate_desc" => query.OrderByDescending(l => l.StartsAt),
                _ => query.OrderByDescending(l => l.StartsAt)
            };

            var data = await query
                .Skip((request.Page - 1) * request.PageSize)
                .Take(request.PageSize)
                .ToListAsync();

            return (data, totalCount);
        }

        private static IQueryable<CareLog> ApplyCareLogDateRange(IQueryable<CareLog> query, PaginationRequest request)
        {
            var rangeRequest = request.ToDateRangeRequest();

            if (rangeRequest.StartDate.HasValue || rangeRequest.EndDate.HasValue)
            {
                return query.ApplyDateRange(request, l => l.StartsAt);
            }

            var utcToday = DateTime.UtcNow.Date;
            var startDateUtc = DateTime.SpecifyKind(utcToday.AddDays(-60), DateTimeKind.Utc);
            var endDateExclusiveUtc = DateTime.SpecifyKind(utcToday.AddDays(61), DateTimeKind.Utc);

            return query.Where(l => l.StartsAt >= startDateUtc && l.StartsAt < endDateExclusiveUtc);
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
