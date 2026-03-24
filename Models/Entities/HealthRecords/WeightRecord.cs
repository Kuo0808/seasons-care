using System;

namespace SeasonsCare.Api.Models.Entities.HealthRecords
{
    public class WeightRecord : IMultiTenantEntity, ISoftDeleteEntity
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public Guid CareGroupId { get; set; }
        
        public decimal Value { get; set; } // 體重 (kg)
        
        public string? Notes { get; set; }
        public DateTime RecordDate { get; set; }
        
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
        public string CreatedBy { get; set; } = string.Empty;
        public DateTime? DeletedAt { get; set; }
        
        public virtual CareGroup? CareGroup { get; set; }
    }
}
