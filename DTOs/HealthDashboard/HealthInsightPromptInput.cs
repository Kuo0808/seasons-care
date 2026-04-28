using System;
using System.Collections.Generic;

namespace SeasonsCare.Api.DTOs.HealthDashboard
{
    public class HealthInsightPromptInput
    {
        public Guid CareGroupId { get; set; }

        // 該照護群組登記的被照顧人姓名／稱呼（CareGroup.RecipientName）
        // 用於 AI 文案中的稱呼，不可由 AI 自行編造姓氏
        public string RecipientName { get; set; } = string.Empty;

        public DateTime DateFrom { get; set; }

        public DateTime DateTo { get; set; }

        public int TotalRecordCount { get; set; }

        public int TodayRecordCount { get; set; }

        public string TodaySummary { get; set; } = string.Empty;

        public string BloodPressureSummary { get; set; } = string.Empty;

        public string BloodSugarSummary { get; set; } = string.Empty;

        public string WeightSummary { get; set; } = string.Empty;

        public string TemperatureSummary { get; set; } = string.Empty;

        public string BloodOxygenSummary { get; set; } = string.Empty;

        public string ClinicalSummary { get; set; } = string.Empty;

        public string NarrativeDirective { get; set; } = string.Empty;

        public List<string> FewShotScenarios { get; set; } = new();

        public List<HealthPriorityFindingPromptDto> PriorityFindings { get; set; } = new();
    }

    public class HealthPriorityFindingPromptDto
    {
        public string MetricType { get; set; } = "general";

        public string Severity { get; set; } = "low";

        public string Confidence { get; set; } = "low";

        public string Title { get; set; } = string.Empty;

        public string Evidence { get; set; } = string.Empty;

        public string Assessment { get; set; } = string.Empty;

        public string SuggestedFocus { get; set; } = string.Empty;

        public bool IsMultiMetric { get; set; }
    }
}
