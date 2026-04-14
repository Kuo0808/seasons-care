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
    /// <summary>
    /// 健康儀表板 API。
    /// </summary>
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
        [EndpointSummary("取得本週健康洞察")]
        [EndpointDescription("依據 path 參數 careGroupId 取得指定照護群組本週的健康儀表板洞察摘要，包含 AI 生成的重點整理與提醒。")]
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
        [EndpointSummary("取得今日健康洞察")]
        [EndpointDescription("依據 path 參數 careGroupId 取得指定照護群組今日的健康儀表板洞察，回傳今日重點與 AI 生成說明。")]
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
        [EndpointSummary("取得健康趨勢總覽")]
        [EndpointDescription("依據 path 參數 careGroupId 取得照護群組的健康趨勢總覽，整合多項健康指標的近期變化與概覽資訊。")]
        public async Task<IActionResult> GetTrendOverview(Guid careGroupId)
        {
            var result = await _healthDashboardService.GetTrendOverviewAsync(_currentUserService.UserId, careGroupId);
            return Ok(new ApiResponse<HealthDashboardTrendOverviewResponse>(result, "取得趨勢概覽成功", HttpContext.TraceIdentifier));
        }

        /// <summary>
        /// 查看過往歷史的 AI 分析報告
        /// </summary>
        [HttpGet("history")]
        [ProducesResponseType(typeof(ApiResponse<DTOs.Common.PagedResponse<HealthDashboardHistoryItemResponse>>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [EndpointSummary("取得健康儀表板歷史紀錄")]
        [EndpointDescription("依據 path 參數 careGroupId 與 query string 的 page、pageSize 取得健康儀表板歷史資料，回傳分頁後的洞察紀錄清單。")]
        public async Task<IActionResult> GetHistory(Guid careGroupId, [FromQuery] GetHealthDashboardHistoryRequest request)
        {
            var result = await _healthDashboardService.GetHistoryAsync(_currentUserService.UserId, careGroupId, request.Page, request.PageSize);
            return Ok(new ApiResponse<DTOs.Common.PagedResponse<HealthDashboardHistoryItemResponse>>(result, "取得報告歷史紀錄成功", HttpContext.TraceIdentifier));
        }

        /// <summary>
        /// 匯出完整健康報告為 PDF (WIP)
        /// </summary>
        [HttpGet("export-pdf")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [EndpointSummary("匯出健康報表 PDF")]
        [EndpointDescription("依據 path 參數 careGroupId 匯出照護群組的健康報表 PDF。目前為預留中的 WIP 端點，先回傳暫時性的 PDF 內容。")]
        public async Task<IActionResult> ExportPdf(Guid careGroupId)
        {
            // 由於匯出 PDF 需要較大的第三方依賴庫（如 QuestPDF），此為預留在未來實作的架構洞口。
            // 目前先回傳一份代表性的文字檔模擬，確認串接流程順暢後再實作實體 PDF 生成。
            
            // Validate access
            await _healthDashboardService.GetWeeklyInsightAsync(_currentUserService.UserId, careGroupId);

            var dummyPdfBytes = System.Text.Encoding.UTF8.GetBytes("This is a dummy PDF file representing the future PDF export feature. Please implement using QuestPDF.");
            return File(dummyPdfBytes, "application/pdf", $"HealthReport_{careGroupId}_{DateTime.UtcNow:yyyyMMdd}.pdf");
        }
    }
}
