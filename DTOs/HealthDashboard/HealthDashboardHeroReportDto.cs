namespace SeasonsCare.Api.DTOs.HealthDashboard
{
    /// <summary>
    /// 每週健康儀表板頂部主報告區塊。
    /// </summary>
    public class HealthDashboardHeroReportDto
    {
        /// <summary>
        /// AI 分析卡片頂部顯示的簡短報告標題。
        /// </summary>
        public string Headline { get; set; } = string.Empty;

        /// <summary>
        /// 補充主標題內容的報告內文。
        /// </summary>
        public string Body { get; set; } = string.Empty;

        /// <summary>
        /// 提供前端決定樣式語氣的提示，例如 supportive、neutral、watchful。
        /// </summary>
        public string Tone { get; set; } = "supportive";

        /// <summary>
        /// 報告內容的可信度等級，例如 low、medium、high。
        /// </summary>
        public string Confidence { get; set; } = "low";
    }
}
