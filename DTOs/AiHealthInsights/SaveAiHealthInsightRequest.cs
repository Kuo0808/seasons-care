using System;

namespace SeasonsCare.Api.DTOs.AiHealthInsights
{
    /// <summary>
    /// 前端完成 AI 分析後，回寫健康洞察結果的 request body。
    /// </summary>
    public class SaveAiHealthInsightRequest
    {
        public string ReportType { get; set; } = string.Empty;

        public DateTime DateFrom { get; set; }

        public DateTime DateTo { get; set; }

        public string OverallSummary { get; set; } = string.Empty;

        public string KeyInsights { get; set; } = string.Empty;

        public string Recommendations { get; set; } = string.Empty;

        public string? SourceDataHash { get; set; }

        public string? ModelName { get; set; }

        public string? PromptVersion { get; set; }
    }
}
