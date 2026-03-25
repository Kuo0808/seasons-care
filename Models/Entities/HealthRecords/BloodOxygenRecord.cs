using System;

namespace SeasonsCare.Api.Models.Entities.HealthRecords
{
    public class BloodOxygenRecord : IMultiTenantEntity, ISoftDeleteEntity
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public Guid CareGroupId { get; set; }
        
        public decimal SpO2 { get; set; } // 血氧飽和度 (%)
        
        public string? Notes { get; set; }
        public DateTime RecordDate { get; set; }
        
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
        public string CreatedBy { get; set; } = string.Empty;
        public DateTime? DeletedAt { get; set; }
        
        public virtual CareGroup? CareGroup { get; set; }
    }
}
