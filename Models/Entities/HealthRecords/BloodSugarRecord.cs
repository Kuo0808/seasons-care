using System;
using System.ComponentModel.DataAnnotations;
using SeasonsCare.Api.Config;

namespace SeasonsCare.Api.Models.Entities.HealthRecords
{
    public class BloodSugarRecord : IMultiTenantEntity, ISoftDeleteEntity
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public Guid CareGroupId { get; set; }
        
        public decimal GlucoseLevel { get; set; } // 血糖值 mg/dL
        public string MeasurementContext { get; set; } = string.Empty; // 量測情境 (飯前/飯後)
        
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
