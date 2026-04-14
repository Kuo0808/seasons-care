namespace SeasonsCare.Api.DTOs.HealthDashboard
{
    public class HealthDashboardTrendPointResponse
    {
        public string Date { get; set; } = string.Empty;

        public decimal? Value { get; set; }
    }
}
