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

        public DateTimeOffset DateFrom { get; set; }

        public DateTimeOffset DateTo { get; set; }

        public string OverallSummary { get; set; } = string.Empty;

        public string TodaySummary { get; set; } = string.Empty;

        public string KeyInsights { get; set; } = string.Empty;

        public string Recommendations { get; set; } = string.Empty;

        public string? TrendLabels { get; set; }

        public string? SourceDataHash { get; set; }

        public string? ModelName { get; set; }

        public string? PromptVersion { get; set; }

        public DateTimeOffset GeneratedAt { get; set; }

        public DateTimeOffset CreatedAt { get; set; }

        public DateTimeOffset? UpdatedAt { get; set; }

        public string CreatedBy { get; set; } = string.Empty;
    }
}
