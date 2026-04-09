using System;
using System.Threading.Tasks;
using SeasonsCare.Api.DTOs.AiHealthInsights;

namespace SeasonsCare.Api.Services
{
    public interface IAiHealthInsightService
    {
        Task<AiHealthInsightResponse> SaveInsightAsync(Guid currentUserId, Guid careGroupId, SaveAiHealthInsightRequest request);
        Task<AiHealthInsightResponse> GetLatestInsightAsync(Guid currentUserId, Guid careGroupId, string? reportType);
    }
}
