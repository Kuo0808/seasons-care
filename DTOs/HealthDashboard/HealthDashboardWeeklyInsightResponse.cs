using System;

namespace SeasonsCare.Api.DTOs.HealthDashboard
{
    public class HealthDashboardWeeklyInsightResponse
    {
        public string OverallSummary { get; set; } = string.Empty;

        public string KeyInsight { get; set; } = string.Empty;

        public string ActionSuggestion { get; set; } = string.Empty;

        public DateTime DateFrom { get; set; }

        public DateTime DateTo { get; set; }

        public bool IsFromCache { get; set; }
    }
}
