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
    /// 健康儀表板相關 API。
    /// 提供每週 AI 分析報告、今日健康摘要、近七天趨勢總覽，以及報告歷史紀錄。
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

        /// <summary>
        /// 取得近七天 AI 健康分析報告。
        /// 回傳首屏報告（heroReport）、關鍵數據洞察（keyInsightSection）、健康行動建議（actionSuggestionSection）。
        /// 若已有快取報告則直接回傳，否則呼叫 AI 即時產生並存入快取。
        /// AI 失敗時會回傳 fallback 內容，可透過 meta.isFallback 判斷。
        /// </summary>
        /// <param name="careGroupId">照護群組 ID</param>
        [HttpGet("weekly-insight")]
        [ProducesResponseType(typeof(ApiResponse<HealthDashboardWeeklyInsightResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [EndpointSummary("取得每週 AI 健康分析報告")]
        [EndpointDescription("回傳近七天的 AI 健康分析，包含首屏報告、關鍵洞察、行動建議與提醒。")]
        public async Task<IActionResult> GetWeeklyInsight(Guid careGroupId)
        {
            var currentUserId = _currentUserService.UserId;
            var result = await _healthDashboardService.GetWeeklyInsightAsync(currentUserId, careGroupId);
            return Ok(new ApiResponse<HealthDashboardWeeklyInsightResponse>(
                result, "已成功取得每週健康分析報告。", HttpContext.TraceIdentifier));
        }

        /// <summary>
        /// 取得今日健康摘要。
        /// 即時統計今日各項健康指標的量測筆數與最新值，不依賴 AI。
        /// 回傳摘要卡片（cards）供前端今日區塊使用。
        /// </summary>
        /// <param name="careGroupId">照護群組 ID</param>
        [HttpGet("today-insight")]
        [ProducesResponseType(typeof(ApiResponse<HealthDashboardTodayInsightResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [EndpointSummary("取得今日健康摘要")]
        [EndpointDescription("即時統計今日各指標量測狀態，回傳摘要卡片與任務進度，不呼叫 AI。")]
        public async Task<IActionResult> GetTodayInsight(Guid careGroupId)
        {
            var currentUserId = _currentUserService.UserId;
            var result = await _healthDashboardService.GetTodayInsightAsync(currentUserId, careGroupId);
            return Ok(new ApiResponse<HealthDashboardTodayInsightResponse>(
                result, "已成功取得今日健康摘要。", HttpContext.TraceIdentifier));
        }

        /// <summary>
        /// 取得近七天健康趨勢總覽。
        /// 即時統計各指標的平均值、最新值、變化量與每日圖表資料點，不依賴 AI。
        /// 趨勢狀態標籤優先使用已快取的 AI 結果，無快取時由規則判斷。
        /// </summary>
        /// <param name="careGroupId">照護群組 ID</param>
        [HttpGet("trend-overview")]
        [ProducesResponseType(typeof(ApiResponse<HealthDashboardTrendOverviewResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [EndpointSummary("取得近七天趨勢總覽")]
        [EndpointDescription("即時統計近七天各指標趨勢卡片與折線圖資料，不呼叫 AI。")]
        public async Task<IActionResult> GetTrendOverview(Guid careGroupId)
        {
            var result = await _healthDashboardService.GetTrendOverviewAsync(_currentUserService.UserId, careGroupId);
            return Ok(new ApiResponse<HealthDashboardTrendOverviewResponse>(
                result, "已成功取得近七天趨勢總覽。", HttpContext.TraceIdentifier));
        }

        /// <summary>
        /// 取得過往每週 AI 報告的分頁歷史紀錄。
        /// </summary>
        /// <param name="careGroupId">照護群組 ID</param>
        /// <param name="request">分頁查詢參數（page、pageSize）</param>
        [HttpGet("history")]
        [ProducesResponseType(typeof(ApiResponse<PagedResponse<HealthDashboardHistoryItemResponse>>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [EndpointSummary("取得儀表板報告歷史紀錄")]
        [EndpointDescription("回傳指定照護群組每週 AI 健康報告的分頁歷史資料。")]
        public async Task<IActionResult> GetHistory(Guid careGroupId, [FromQuery] GetHealthDashboardHistoryRequest request)
        {
            var result = await _healthDashboardService.GetHistoryAsync(
                _currentUserService.UserId, careGroupId, request.Page, request.PageSize);
            return Ok(new ApiResponse<PagedResponse<HealthDashboardHistoryItemResponse>>(
                result, "已成功取得健康儀表板報告歷史紀錄。", HttpContext.TraceIdentifier));
        }

        /// <summary>
        /// 匯出健康儀表板報告 PDF（開發中）。
        /// 目前會先驗證權限並回傳暫時性的 PDF 內容，完整匯出功能待後續實作。
        /// </summary>
        /// <param name="careGroupId">照護群組 ID</param>
        [HttpGet("export-pdf")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [EndpointSummary("匯出健康儀表板 PDF")]
        [EndpointDescription("目前會先驗證權限並回傳暫時性的 PDF 內容，完整匯出功能待後續實作。")]
        public async Task<IActionResult> ExportPdf(Guid careGroupId)
        {
            await _healthDashboardService.GetWeeklyInsightAsync(_currentUserService.UserId, careGroupId);

            var dummyPdfBytes = System.Text.Encoding.UTF8.GetBytes(
                "這是暫時性的 PDF 內容，代表未來的健康報告匯出功能，後續請改用 QuestPDF 實作。");
            return File(dummyPdfBytes, "application/pdf",
                $"HealthReport_{careGroupId}_{DateTime.UtcNow:yyyyMMdd}.pdf");
        }
    }
}
