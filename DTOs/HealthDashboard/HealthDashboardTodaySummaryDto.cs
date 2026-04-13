using System;

namespace SeasonsCare.Api.DTOs.HealthDashboard
{
    public class HealthDashboardTodaySummaryDto
    {
        public string SummaryText { get; set; } = string.Empty;

        public int RecordCount { get; set; }

        public DateTime? LatestRecordAt { get; set; }
    }
}
