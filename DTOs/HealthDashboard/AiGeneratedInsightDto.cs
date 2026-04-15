using System;
using System.Collections.Generic;

namespace SeasonsCare.Api.DTOs.HealthDashboard
{
    /// <summary>
    /// AI 生成後的內部資料模型。
    /// 會先在服務層使用，再映射成 API 回應與資料庫欄位。
    /// </summary>
    public class AiGeneratedInsightDto
    {
        /// <summary>
        /// 舊版相容用的整體摘要短文。
        /// </summary>
        public string OverallSummary { get; set; } = string.Empty;

        /// <summary>
        /// 舊版相容用的今日摘要短文。
        /// </summary>
        public string TodaySummary { get; set; } = string.Empty;

        /// <summary>
        /// 舊版相容用的關鍵洞察短文。
        /// </summary>
        public string KeyInsights { get; set; } = string.Empty;

        /// <summary>
        /// 舊版相容用的建議短文。
        /// </summary>
        public string Recommendations { get; set; } = string.Empty;

        /// <summary>
        /// 各健康指標的趨勢狀態標籤。
        /// </summary>
        public TrendLabelsDto? TrendLabels { get; set; }

        /// <summary>
        /// 首屏 AI 分析報告區塊。
        /// </summary>
        public HealthDashboardHeroReportDto? HeroReport { get; set; }

        /// <summary>
        /// 關鍵數據洞察區塊。
        /// </summary>
        public HealthDashboardInsightSectionDto? KeyInsightSection { get; set; }

        /// <summary>
        /// 健康行動建議區塊。
        /// </summary>
        public HealthDashboardActionSectionDto? ActionSuggestionSection { get; set; }

        /// <summary>
        /// 今日健康摘要卡片集合。
        /// </summary>
        public List<HealthDashboardTodayCardDto> TodayCards { get; set; } = new();

        /// <summary>
        /// 額外提醒或資料缺口資訊。
        /// </summary>
        public List<HealthDashboardAlertDto> Alerts { get; set; } = new();

        /// <summary>
        /// 來源事實資料的雜湊值，用於辨識同一份輸入資料。
        /// </summary>
        public string? SourceDataHash { get; set; }

        /// <summary>
        /// 生成此內容所使用的模型名稱。
        /// </summary>
        public string? ModelName { get; set; }

        /// <summary>
        /// 生成此內容所使用的 prompt 版本。
        /// </summary>
        public string? PromptVersion { get; set; }

        /// <summary>
        /// AI 生成完成時間，為 UTC。
        /// </summary>
        public DateTime GeneratedAt { get; set; }
    }
}
