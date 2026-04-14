using System;
using System.Collections.Generic;

namespace SeasonsCare.Api.DTOs.HealthDashboard
{
    public class HealthDashboardTrendOverviewResponse
    {
        public DateTime DateFrom { get; set; }

        public DateTime DateTo { get; set; }

        public List<HealthDashboardTrendCardResponse> Metrics { get; set; } = new();
    }
}
