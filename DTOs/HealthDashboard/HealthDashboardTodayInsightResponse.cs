using System;

namespace SeasonsCare.Api.DTOs.HealthDashboard
{
    public class HealthDashboardTodayInsightResponse
    {
        public string Summary { get; set; } = string.Empty;

        public bool HasTodayRecords { get; set; }

        public int RecordCount { get; set; }

        public DateTime? LatestRecordAt { get; set; }
    }
}
