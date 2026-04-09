using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using SeasonsCare.Api.Data;
using SeasonsCare.Api.Models.Entities;

namespace SeasonsCare.Api.Repositories
{
    public class AiHealthInsightRepository : IAiHealthInsightRepository
    {
        private readonly ApplicationDbContext _context;

        public AiHealthInsightRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<AiHealthInsight?> GetByUniqueKeyAsync(Guid careGroupId, string reportType, DateTime dateFrom, DateTime dateTo)
        {
            return await _context.Set<AiHealthInsight>()
                .FirstOrDefaultAsync(x =>
                    x.CareGroupId == careGroupId &&
                    x.ReportType == reportType &&
                    x.DateFrom == dateFrom &&
                    x.DateTo == dateTo);
        }

        public async Task<AiHealthInsight?> GetLatestAsync(Guid careGroupId, string? reportType)
        {
            var query = _context.Set<AiHealthInsight>()
                .Where(x => x.CareGroupId == careGroupId);

            if (!string.IsNullOrWhiteSpace(reportType))
            {
                query = query.Where(x => x.ReportType == reportType);
            }

            return await query
                .OrderByDescending(x => x.GeneratedAt)
                .ThenByDescending(x => x.CreatedAt)
                .FirstOrDefaultAsync();
        }

        public async Task AddAsync(AiHealthInsight insight)
        {
            await _context.Set<AiHealthInsight>().AddAsync(insight);
        }

        public async Task UpdateAsync(AiHealthInsight insight)
        {
            _context.Set<AiHealthInsight>().Update(insight);
            await Task.CompletedTask;
        }

        public async Task SaveChangesAsync()
        {
            await _context.SaveChangesAsync();
        }
    }
}
