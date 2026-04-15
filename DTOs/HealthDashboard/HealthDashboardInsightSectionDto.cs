namespace SeasonsCare.Api.DTOs.HealthDashboard
{
    /// <summary>
    /// 用於呈現每週最重要洞察的結構化區塊。
    /// </summary>
    public class HealthDashboardInsightSectionDto
    {
        /// <summary>
        /// 顯示在前端區塊上的標籤文字。
        /// </summary>
        public string Label { get; set; } = string.Empty;

        /// <summary>
        /// 說明重要變化或模式的洞察內文。
        /// </summary>
        public string Body { get; set; } = string.Empty;

        /// <summary>
        /// 此洞察主要對應的健康指標，例如 blood_pressure 或 blood_sugar。
        /// </summary>
        public string MetricType { get; set; } = string.Empty;

        /// <summary>
        /// 此洞察的嚴重程度提示，例如 low、medium、high。
        /// </summary>
        public string Severity { get; set; } = "low";
    }
}
