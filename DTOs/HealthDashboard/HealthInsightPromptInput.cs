using System;

namespace SeasonsCare.Api.DTOs.HealthDashboard
{
    public class HealthInsightPromptInput
    {
        public Guid CareGroupId { get; set; }

        public DateTime DateFrom { get; set; }

        public DateTime DateTo { get; set; }

        public int TotalRecordCount { get; set; }

        public int TodayRecordCount { get; set; }

        public string TodaySummary { get; set; } = string.Empty;

        public string BloodPressureSummary { get; set; } = string.Empty;

        public string BloodSugarSummary { get; set; } = string.Empty;

        public string WeightSummary { get; set; } = string.Empty;

        public string TemperatureSummary { get; set; } = string.Empty;

        public string BloodOxygenSummary { get; set; } = string.Empty;
    }
}
