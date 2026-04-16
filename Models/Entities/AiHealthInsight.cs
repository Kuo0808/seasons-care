using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SeasonsCare.Api.Config;

namespace SeasonsCare.Api.Models.Entities
{
    /// <summary>
    /// 儲存由前端 AI 分析後回寫的健康洞察快照。
    /// 這筆資料不屬於原始量測值，而是某段時間的分析結果。
    /// </summary>
    public class AiHealthInsight : IMultiTenantEntity, ISoftDeleteEntity
    {
        public Guid Id { get; set; } = Guid.NewGuid();

        [Required]
        [MaxLength(50)]
        public string ReportType { get; set; } = string.Empty;

        [Required]
        public Guid CareGroupId { get; set; }

        [ForeignKey(nameof(CareGroupId))]
        public CareGroup CareGroup { get; set; } = null!;

        public DateTime DateFrom { get; set; }

        public DateTime DateTo { get; set; }

        [Required]
        public string OverallSummary { get; set; } = string.Empty;

        [Required]
        public string TodaySummary { get; set; } = string.Empty;

        [Required]
        public string KeyInsights { get; set; } = string.Empty;

        [Required]
        public string Recommendations { get; set; } = string.Empty;

        /// <summary>
        /// AI 產出的各指標趨勢狀態標籤，以 JSON 字串儲存。
        /// </summary>
        public string? TrendLabels { get; set; }

        /// <summary>
        /// AI 產出的完整結構化結果 JSON，包含 heroReport、keyInsightSection、actionSuggestionSection、todayCards、alerts。
        /// </summary>
        public string? ResultJson { get; set; }

        [MaxLength(128)]
        public string? SourceDataHash { get; set; }

        [MaxLength(100)]
        public string? ModelName { get; set; }

        [MaxLength(50)]
        public string? PromptVersion { get; set; }

public DateTime GeneratedAt { get; set; } = TimeHelper.UtcNow;

public DateTime CreatedAt { get; set; } = TimeHelper.UtcNow;

        [ConcurrencyCheck]
        public DateTime? UpdatedAt { get; set; }

        public string CreatedBy { get; set; } = string.Empty;

        public DateTime? DeletedAt { get; set; }
    }
}
