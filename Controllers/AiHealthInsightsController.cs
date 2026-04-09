using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using SeasonsCare.Api.DTOs.AiHealthInsights;
using SeasonsCare.Api.DTOs.Common;
using SeasonsCare.Api.Services;

namespace SeasonsCare.Api.Controllers
{
    /// <summary>
    /// AI 健康分析快照 API。
    /// 前端完成 AI 分析後可將結果回寫，首頁或分析頁則可直接讀取已儲存的快照。
    /// </summary>
    [Authorize]
    [ApiController]
    [Route("api/care-groups/{careGroupId}/ai-insights")]
    public class AiHealthInsightsController : ControllerBase
    {
        private readonly IAiHealthInsightService _aiHealthInsightService;
        private readonly ICurrentUserService _currentUserService;

        public AiHealthInsightsController(IAiHealthInsightService aiHealthInsightService, ICurrentUserService currentUserService)
        {
            _aiHealthInsightService = aiHealthInsightService;
            _currentUserService = currentUserService;
        }

        /// <summary>
        /// 儲存前端產生的 AI 健康分析結果。
        /// 若同一 reportType 與分析區間已存在，則覆蓋為最新內容。
        /// </summary>
        [HttpPost]
        [ProducesResponseType(typeof(ApiResponse<AiHealthInsightResponse>), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> SaveInsight(Guid careGroupId, [FromBody] SaveAiHealthInsightRequest request)
        {
            var currentUserId = _currentUserService.UserId;
            var result = await _aiHealthInsightService.SaveInsightAsync(currentUserId, careGroupId, request);
            var response = new ApiResponse<AiHealthInsightResponse>(result, "儲存 AI 健康分析成功", HttpContext.TraceIdentifier);
            return StatusCode(201, response);
        }

        /// <summary>
        /// 取得指定照護群組最新的 AI 健康分析快照。
        /// 可選擇以 reportType 篩選。
        /// </summary>
        [HttpGet("latest")]
        [ProducesResponseType(typeof(ApiResponse<AiHealthInsightResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetLatestInsight(Guid careGroupId, [FromQuery] GetLatestAiHealthInsightRequest request)
        {
            var currentUserId = _currentUserService.UserId;
            var result = await _aiHealthInsightService.GetLatestInsightAsync(currentUserId, careGroupId, request.ReportType);
            var response = new ApiResponse<AiHealthInsightResponse>(result, "取得 AI 健康分析成功", HttpContext.TraceIdentifier);
            return Ok(response);
        }
    }
}
