using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using SeasonsCare.Api.DTOs.Common;
using SeasonsCare.Api.DTOs.HealthDashboard;
using SeasonsCare.Api.Services;
using SeasonsCare.Api.Services.HealthDashboard;

namespace SeasonsCare.Api.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/care-groups/{careGroupId}/health-dashboard")]
    public class HealthDashboardController : ControllerBase
    {
        private readonly IHealthDashboardService _healthDashboardService;
        private readonly ICurrentUserService _currentUserService;

        public HealthDashboardController(IHealthDashboardService healthDashboardService, ICurrentUserService currentUserService)
        {
            _healthDashboardService = healthDashboardService;
            _currentUserService = currentUserService;
        }

        [HttpGet("weekly-insight")]
        [ProducesResponseType(typeof(ApiResponse<HealthDashboardWeeklyInsightResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> GetWeeklyInsight(Guid careGroupId)
        {
            var currentUserId = _currentUserService.UserId;
            var result = await _healthDashboardService.GetWeeklyInsightAsync(currentUserId, careGroupId);
            var response = new ApiResponse<HealthDashboardWeeklyInsightResponse>(result, "取得近七天 AI 分析成功。", HttpContext.TraceIdentifier);
            return Ok(response);
        }

        [HttpGet("today-insight")]
        [ProducesResponseType(typeof(ApiResponse<HealthDashboardTodayInsightResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> GetTodayInsight(Guid careGroupId)
        {
            var currentUserId = _currentUserService.UserId;
            var result = await _healthDashboardService.GetTodayInsightAsync(currentUserId, careGroupId);
            var response = new ApiResponse<HealthDashboardTodayInsightResponse>(result, "取得今日健康摘要成功。", HttpContext.TraceIdentifier);
            return Ok(response);
        }

        [HttpGet("trend-overview")]
        [ProducesResponseType(typeof(ApiResponse<HealthDashboardTrendOverviewResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> GetTrendOverview(Guid careGroupId)
        {
            var currentUserId = _currentUserService.UserId;
            var result = await _healthDashboardService.GetTrendOverviewAsync(currentUserId, careGroupId);
            var response = new ApiResponse<HealthDashboardTrendOverviewResponse>(result, "取得近七天健康趨勢成功。", HttpContext.TraceIdentifier);
            return Ok(response);
        }
    }
}
