using System.Collections.Generic;

namespace SeasonsCare.Api.DTOs.HealthDashboard
{
    public class HealthDashboardTrendsDto
    {
        public List<BloodPressureTrendPointDto> BloodPressures { get; set; } = new();

        public List<BloodSugarTrendPointDto> BloodSugars { get; set; } = new();

        public List<SingleValueTrendPointDto> Weights { get; set; } = new();

        public List<SingleValueTrendPointDto> Temperatures { get; set; } = new();

        public List<SingleValueTrendPointDto> BloodOxygens { get; set; } = new();
    }
}
