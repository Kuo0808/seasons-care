namespace SeasonsCare.Api.DTOs.HealthDashboard
{
    /// <summary>
    /// AI 產出的各指標趨勢狀態描述，例如「趨勢良好」、「建議觀察」
    /// 。
    /// </summary>
    public class TrendLabelsDto
    {
        public string BloodPressure { get; set; } = string.Empty;

        public string BloodOxygen { get; set; } = string.Empty;

        public string BloodSugar { get; set; } = string.Empty;

        public string Temperature { get; set; } = string.Empty;

        public string Weight { get; set; } = string.Empty;
    }
}
