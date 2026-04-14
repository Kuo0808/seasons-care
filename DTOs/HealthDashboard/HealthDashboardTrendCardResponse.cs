using System.Collections.Generic;

namespace SeasonsCare.Api.DTOs.HealthDashboard
{
    public class HealthDashboardTrendCardResponse
    {
        public string MetricType { get; set; } = string.Empty;

        public string Title { get; set; } = string.Empty;

        public string StatusLabel { get; set; } = string.Empty;

        public string DisplayValue { get; set; } = string.Empty;

        public string Unit { get; set; } = string.Empty;

        public decimal? AverageValue { get; set; }

        public decimal? SecondaryAverageValue { get; set; }

        public List<HealthDashboardTrendPointResponse> Points { get; set; } = new();

        public List<HealthDashboardTrendPointResponse>? SecondaryPoints { get; set; }
    }
}
