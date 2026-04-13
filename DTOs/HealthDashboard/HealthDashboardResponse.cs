using System;

namespace SeasonsCare.Api.DTOs.HealthDashboard
{
    public class HealthDashboardResponse
    {
        public AiGeneratedInsightDto? AiReport { get; set; }

        public HealthDashboardTodaySummaryDto TodaySummary { get; set; } = new();

        public HealthDashboardTrendsDto Trends { get; set; } = new();

        public DateTime DateFrom { get; set; }

        public DateTime DateTo { get; set; }

        public TrendLabelsDto? TrendLabels { get; set; }

        public bool IsFromCache { get; set; }
    }
}
