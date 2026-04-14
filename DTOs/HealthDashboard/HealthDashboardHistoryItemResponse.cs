using System;

namespace SeasonsCare.Api.DTOs.HealthDashboard
{
    public class HealthDashboardHistoryItemResponse
    {
        public Guid Id { get; set; }
        public DateTime DateFrom { get; set; }
        public DateTime DateTo { get; set; }
        public string OverallSummary { get; set; } = string.Empty;
        public DateTime GeneratedAt { get; set; }
    }
}
