using System;
using System.Collections.Generic;

namespace SeasonsCare.Api.DTOs.HealthDashboard
{
    public class HealthDashboardTrendOverviewResponse
    {
        public DateTimeOffset DateFrom { get; set; }

        public DateTimeOffset DateTo { get; set; }

        public List<HealthDashboardTrendCardResponse> Metrics { get; set; } = new();
    }
}
