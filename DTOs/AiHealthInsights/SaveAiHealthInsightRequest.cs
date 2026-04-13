using System;

namespace SeasonsCare.Api.DTOs.AiHealthInsights
{
    public class SaveAiHealthInsightRequest
    {
        /// <summary>
        /// 必填。報告類型，最長 50 字。
        /// </summary>
        public string ReportType { get; set; } = string.Empty;

        /// <summary>
        /// 必填。分析起始時間。
        /// </summary>
        public DateTime DateFrom { get; set; }

        /// <summary>
        /// 必填。分析結束時間。
        /// </summary>
        public DateTime DateTo { get; set; }

        /// <summary>
        /// 必填。整體摘要。
        /// </summary>
        public string OverallSummary { get; set; } = string.Empty;

        /// <summary>
        /// 必填。今日摘要。
        /// </summary>
        public string TodaySummary { get; set; } = string.Empty;

        /// <summary>
        /// 必填。關鍵洞察。
        /// </summary>
        public string KeyInsights { get; set; } = string.Empty;

        /// <summary>
        /// 必填。建議內容。
        /// </summary>
        public string Recommendations { get; set; } = string.Empty;

        /// <summary>
        /// 選填。來源資料雜湊，最長 128 字。
        /// </summary>
        public string? SourceDataHash { get; set; }

        /// <summary>
        /// 選填。模型名稱，最長 100 字。
        /// </summary>
        public string? ModelName { get; set; }

        /// <summary>
        /// 選填。Prompt 版本，最長 50 字。
        /// </summary>
        public string? PromptVersion { get; set; }
    }
}
