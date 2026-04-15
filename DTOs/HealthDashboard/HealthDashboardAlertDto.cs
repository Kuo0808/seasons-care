namespace SeasonsCare.Api.DTOs.HealthDashboard
{
    /// <summary>
    /// 用於提醒、資料缺口或需持續觀察情況的警示項目。
    /// </summary>
    public class HealthDashboardAlertDto
    {
        /// <summary>
        /// 警示類型識別值，例如 data_gap、reminder、observation。
        /// </summary>
        public string Type { get; set; } = string.Empty;

        /// <summary>
        /// 顯示給使用者的警示訊息。
        /// </summary>
        public string Message { get; set; } = string.Empty;

        /// <summary>
        /// 提供前端決定警示強度的提示。
        /// </summary>
        public string Severity { get; set; } = "low";
    }
}
