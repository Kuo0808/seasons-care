using System;
using System.Threading.Tasks;
using SeasonsCare.Api.DTOs.HealthDashboard;

namespace SeasonsCare.Api.Services.HealthDashboard
{
    public interface IHealthDashboardService
    {
        Task<HealthDashboardWeeklyInsightResponse> GetWeeklyInsightAsync(Guid currentUserId, Guid careGroupId);
        Task<HealthDashboardTodayInsightResponse> GetTodayInsightAsync(Guid currentUserId, Guid careGroupId);
        Task<HealthDashboardTrendOverviewResponse> GetTrendOverviewAsync(Guid currentUserId, Guid careGroupId);
        Task<DTOs.Common.PagedResponse<HealthDashboardHistoryItemResponse>> GetHistoryAsync(Guid currentUserId, Guid careGroupId, int page, int pageSize);
    }
}
