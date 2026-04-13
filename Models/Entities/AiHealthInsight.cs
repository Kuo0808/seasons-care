using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

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

        [MaxLength(128)]
        public string? SourceDataHash { get; set; }

        [MaxLength(100)]
        public string? ModelName { get; set; }

        [MaxLength(50)]
        public string? PromptVersion { get; set; }

        public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [ConcurrencyCheck]
        public DateTime? UpdatedAt { get; set; }

        public string CreatedBy { get; set; } = string.Empty;

        public DateTime? DeletedAt { get; set; }
    }
}
