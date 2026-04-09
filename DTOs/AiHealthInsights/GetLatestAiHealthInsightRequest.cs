namespace SeasonsCare.Api.DTOs.AiHealthInsights
{
    /// <summary>
    /// 取得最新 AI 健康洞察時可用的篩選條件。
    /// </summary>
    public class GetLatestAiHealthInsightRequest
    {
        public string? ReportType { get; set; }
    }
}
