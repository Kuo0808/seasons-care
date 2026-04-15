namespace SeasonsCare.Api.DTOs.HealthDashboard
{
    /// <summary>
    /// AI 產出的各健康指標趨勢狀態描述，例如「趨勢良好」或「建議觀察」。
    /// </summary>
    public class TrendLabelsDto
    {
        /// <summary>
        /// 血壓趨勢標籤。
        /// </summary>
        public string BloodPressure { get; set; } = string.Empty;

        /// <summary>
        /// 血氧趨勢標籤。
        /// </summary>
        public string BloodOxygen { get; set; } = string.Empty;

        /// <summary>
        /// 血糖趨勢標籤。
        /// </summary>
        public string BloodSugar { get; set; } = string.Empty;

        /// <summary>
        /// 體溫趨勢標籤。
        /// </summary>
        public string Temperature { get; set; } = string.Empty;

        /// <summary>
        /// 體重趨勢標籤。
        /// </summary>
        public string Weight { get; set; } = string.Empty;
    }
}
