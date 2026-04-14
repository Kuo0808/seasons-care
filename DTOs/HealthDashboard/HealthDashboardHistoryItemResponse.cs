using System;

namespace SeasonsCare.Api.DTOs.HealthDashboard
{
    public class HealthDashboardHistoryItemResponse
    {
        public Guid Id { get; set; }
        public DateTimeOffset DateFrom { get; set; }
        public DateTimeOffset DateTo { get; set; }
        public string OverallSummary { get; set; } = string.Empty;
        public DateTimeOffset GeneratedAt { get; set; }
    }
}
