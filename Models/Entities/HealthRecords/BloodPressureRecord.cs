using System;
using System.ComponentModel.DataAnnotations;
using SeasonsCare.Api.Config;
using SeasonsCare.Api.Models.Entities;

namespace SeasonsCare.Api.Models.Entities.HealthRecords
{
    public class BloodPressureRecord : IMultiTenantEntity, ISoftDeleteEntity
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public Guid CareGroupId { get; set; }
        public int Systolic { get; set; } // 收縮壓
        public int Diastolic { get; set; } // 舒張壓
        public string? Notes { get; set; }
        public DateTime RecordDate { get; set; }
        public DateTime CreatedAt { get; set; } = TimeHelper.Now;
        [ConcurrencyCheck]
        public DateTime UpdatedAt { get; set; } = TimeHelper.Now;
        public string CreatedBy { get; set; } = string.Empty;
        public DateTime? DeletedAt { get; set; }
        
        public virtual CareGroup? CareGroup { get; set; }
    }
}
