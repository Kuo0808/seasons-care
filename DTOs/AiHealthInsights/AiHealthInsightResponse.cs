using System;

namespace SeasonsCare.Api.DTOs.AiHealthInsights
{
    /// <summary>
    /// AI 健康洞察快照回應。
    /// </summary>
    public class AiHealthInsightResponse
    {
        public Guid Id { get; set; }

        public Guid CareGroupId { get; set; }

        public string ReportType { get; set; } = string.Empty;

        public DateTime DateFrom { get; set; }

        public DateTime DateTo { get; set; }

        public string OverallSummary { get; set; } = string.Empty;

        public string TodaySummary { get; set; } = string.Empty;

        public string KeyInsights { get; set; } = string.Empty;

        public string Recommendations { get; set; } = string.Empty;

        public string? TrendLabels { get; set; }

        public string? SourceDataHash { get; set; }

        public string? ModelName { get; set; }

        public string? PromptVersion { get; set; }

        public DateTime GeneratedAt { get; set; }

        public DateTime CreatedAt { get; set; }

        public DateTime? UpdatedAt { get; set; }

        public string CreatedBy { get; set; } = string.Empty;
    }
}
