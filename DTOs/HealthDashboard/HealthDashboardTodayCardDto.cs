namespace SeasonsCare.Api.DTOs.HealthDashboard
{
    /// <summary>
    /// 今日健康摘要區塊使用的卡片資料。
    /// </summary>
    public class HealthDashboardTodayCardDto
    {
        /// <summary>
        /// 顯示在今日摘要卡片頂部的標題。
        /// </summary>
        public string Title { get; set; } = string.Empty;

        /// <summary>
        /// 卡片主體顯示的今日摘要內容。
        /// </summary>
        public string Summary { get; set; } = string.Empty;

        /// <summary>
        /// 顯示任務進度或完成度的補充說明。
        /// </summary>
        public string ProgressNote { get; set; } = string.Empty;

        /// <summary>
        /// 提供前端決定圖示的識別值。
        /// </summary>
        public string IconType { get; set; } = "insight";

        /// <summary>
        /// 提供前端決定樣式語氣的提示，例如 supportive 或 neutral。
        /// </summary>
        public string Tone { get; set; } = "supportive";
    }
}
