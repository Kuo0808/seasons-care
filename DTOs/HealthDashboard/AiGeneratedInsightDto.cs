using System;

namespace SeasonsCare.Api.DTOs.HealthDashboard
{
    public class AiGeneratedInsightDto
    {
        public string OverallSummary { get; set; } = string.Empty;

        public string KeyInsights { get; set; } = string.Empty;

        public string Recommendations { get; set; } = string.Empty;

        public string? SourceDataHash { get; set; }

        public string? ModelName { get; set; }

        public string? PromptVersion { get; set; }

        public DateTime GeneratedAt { get; set; }
    }
}
