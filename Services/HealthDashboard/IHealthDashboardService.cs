using System;
using System.Threading.Tasks;
using SeasonsCare.Api.DTOs.HealthDashboard;

namespace SeasonsCare.Api.Services.HealthDashboard
{
    public interface IHealthDashboardService
    {
        Task<HealthDashboardResponse> GetDashboardAsync(Guid currentUserId, Guid careGroupId);
    }
}
