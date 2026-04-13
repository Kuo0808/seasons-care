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

        /// <summary>
        /// 取得健康儀表板資料。
        /// 一次回傳近 7 天趨勢、今日摘要，以及 AI 分析結果。
        /// </summary>
        [HttpGet]
        [ProducesResponseType(typeof(ApiResponse<HealthDashboardResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> GetDashboard(Guid careGroupId)
        {
            var currentUserId = _currentUserService.UserId;
            var result = await _healthDashboardService.GetDashboardAsync(currentUserId, careGroupId);
            var response = new ApiResponse<HealthDashboardResponse>(result, "取得健康儀表板成功。", HttpContext.TraceIdentifier);
            return Ok(response);
        }
    }
}
