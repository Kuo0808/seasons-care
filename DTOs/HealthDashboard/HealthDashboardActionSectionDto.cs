namespace SeasonsCare.Api.DTOs.HealthDashboard
{
    /// <summary>
    /// 用於描述下一步照護建議的結構化區塊。
    /// </summary>
    public class HealthDashboardActionSectionDto
    {
        /// <summary>
        /// 顯示在前端區塊上的標籤文字。
        /// </summary>
        public string Label { get; set; } = string.Empty;

        /// <summary>
        /// 提供給照護者的行動建議內文。
        /// </summary>
        public string Body { get; set; } = string.Empty;

        /// <summary>
        /// 此建議行動的優先等級提示。
        /// </summary>
        public string Priority { get; set; } = "medium";

        /// <summary>
        /// 建議行動的時間範圍，例如 today 或 this_week。
        /// </summary>
        public string Timeframe { get; set; } = "today";
    }
}
