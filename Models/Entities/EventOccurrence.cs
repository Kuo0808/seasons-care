using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SeasonsCare.Api.Config;
using SeasonsCare.Api.Models.Enums;

namespace SeasonsCare.Api.Models.Entities
{
    /// <summary>
    /// 事件實例。
    /// 用來描述系列事件展開後的每一次實際事件，並保留單次取消或單次覆寫的空間。
    /// </summary>
    public class EventOccurrence : IMultiTenantEntity, ISoftDeleteEntity
    {
        public Guid Id { get; set; } = Guid.NewGuid();

        [Required]
        public Guid EventSeriesId { get; set; }

        [ForeignKey(nameof(EventSeriesId))]
        public EventSeries EventSeries { get; set; } = null!;

        [Required]
        public Guid CareGroupId { get; set; }

        [ForeignKey(nameof(CareGroupId))]
        public CareGroup CareGroup { get; set; } = null!;

        public DateTime ScheduledStartAt { get; set; }

        public DateTime? ScheduledEndAt { get; set; }

        public string? OverrideTitle { get; set; }

        public string? OverrideDescription { get; set; }

        public string[]? OverrideParticipants { get; set; }

        public EventOccurrenceStatus Status { get; set; } = EventOccurrenceStatus.Scheduled;

        public bool IsImportantOverride { get; set; }

        public DateTime CreatedAt { get; set; } = TimeHelper.Now;

        [ConcurrencyCheck]
        public DateTime? UpdatedAt { get; set; }

        public string CreatedBy { get; set; } = string.Empty;

        public DateTime? DeletedAt { get; set; }
    }
}
