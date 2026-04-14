using System;
using System.Threading.Tasks;
using SeasonsCare.Api.Models.Entities;

namespace SeasonsCare.Api.Repositories
{
    public interface IAiHealthInsightRepository
    {
        Task<AiHealthInsight?> GetByUniqueKeyAsync(Guid careGroupId, string reportType, DateTime dateFrom, DateTime dateTo);
        Task<AiHealthInsight?> GetLatestAsync(Guid careGroupId, string? reportType);
        Task<(System.Collections.Generic.IReadOnlyList<AiHealthInsight> Items, int TotalCount)> GetPagedHistoryAsync(Guid careGroupId, string reportType, int page, int pageSize);
        Task AddAsync(AiHealthInsight insight);
        Task UpdateAsync(AiHealthInsight insight);
        Task SaveChangesAsync();
    }
}
