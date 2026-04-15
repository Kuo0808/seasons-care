using System;
using System.Collections.Generic;

namespace SeasonsCare.Api.DTOs.HealthDashboard
{
    /// <summary>
    /// 近七天健康儀表板 AI 分析回應。
    /// 包含首屏報告、關鍵洞察、行動建議與提醒資訊。
    /// </summary>
    public class HealthDashboardWeeklyInsightResponse
    {
        /// <summary>
        /// 報告涵蓋區間起始時間，已轉為台灣時區。
        /// </summary>
        public DateTimeOffset DateFrom { get; set; }

        /// <summary>
        /// 報告涵蓋區間結束時間，已轉為台灣時區。
        /// </summary>
        public DateTimeOffset DateTo { get; set; }

        /// <summary>
        /// 是否直接使用已快取的 AI 報告結果。
        /// </summary>
        public bool IsFromCache { get; set; }

        /// <summary>
        /// 首屏 AI 分析報告區塊，包含標題、內文、語氣與信心等級。
        /// </summary>
        public HealthDashboardHeroReportDto HeroReport { get; set; } = new();

        /// <summary>
        /// 關鍵數據洞察區塊，指出最值得注意的數據變化。
        /// </summary>
        public HealthDashboardInsightSectionDto KeyInsightSection { get; set; } = new();

        /// <summary>
        /// 健康行動建議區塊，提供具體可執行的下一步建議。
        /// </summary>
        public HealthDashboardActionSectionDto ActionSuggestionSection { get; set; } = new();

        /// <summary>
        /// 額外提醒或資料缺口提示。
        /// </summary>
        public List<HealthDashboardAlertDto> Alerts { get; set; } = new();

        /// <summary>
        /// 回應的中繼資訊，例如 fallback 狀態、信心等級與版本資訊。
        /// </summary>
        public HealthDashboardMetaDto Meta { get; set; } = new();
    }
}
