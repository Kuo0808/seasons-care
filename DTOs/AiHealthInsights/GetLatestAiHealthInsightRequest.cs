namespace SeasonsCare.Api.DTOs.AiHealthInsights
{
    public class GetLatestAiHealthInsightRequest
    {
        /// <summary>
        /// 選填。報告類型；省略時由後端回傳最新一筆可用報告。
        /// </summary>
        public string? ReportType { get; set; }
    }
}
