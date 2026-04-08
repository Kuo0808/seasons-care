using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SeasonsCare.Api.Models.Enums;

namespace SeasonsCare.Api.Models.Entities
{
    /// <summary>
    /// 事件系列定義。
    /// 用來描述一個可重複展開的事件規則，例如每週一固定回診。
    /// </summary>
    public class EventSeries : IMultiTenantEntity, ISoftDeleteEntity
    {
        public Guid Id { get; set; } = Guid.NewGuid();

        [Required]
        [MaxLength(100)]
        public string Title { get; set; } = string.Empty;

        public string? Description { get; set; }

        public DateTime StartsAt { get; set; } = DateTime.UtcNow;

        public int? DurationMinutes { get; set; }

        public EventRepeatPattern RepeatPattern { get; set; } = EventRepeatPattern.None;

        public int RepeatInterval { get; set; } = 1;

        /// <summary>
        /// 星期遮罩，使用 DayOfWeek 對應位元:
        /// Sunday=1, Monday=2, Tuesday=4, Wednesday=8, Thursday=16, Friday=32, Saturday=64
        /// </summary>
        public int? DaysOfWeekMask { get; set; }

        public EventSeriesEndType EndType { get; set; } = EventSeriesEndType.Never;

        public DateTime? EndAt { get; set; }

        public int? OccurrenceCount { get; set; }

        public string[] Participants { get; set; } = Array.Empty<string>();

        [MaxLength(50)]
        public string? Status { get; set; }

        public bool IsImportant { get; set; }

        [Required]
        public Guid CareGroupId { get; set; }

        [ForeignKey(nameof(CareGroupId))]
        public CareGroup CareGroup { get; set; } = null!;

        public ICollection<EventOccurrence> Occurrences { get; set; } = new List<EventOccurrence>();

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [ConcurrencyCheck]
        public DateTime? UpdatedAt { get; set; }

        public string CreatedBy { get; set; } = string.Empty;

        public DateTime? DeletedAt { get; set; }
    }
}
