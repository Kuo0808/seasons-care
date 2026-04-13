using System;
using System.ComponentModel.DataAnnotations;
using SeasonsCare.Api.Config;

namespace SeasonsCare.Api.Models.Entities.HealthRecords
{
    public class TemperatureRecord : IMultiTenantEntity, ISoftDeleteEntity
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public Guid CareGroupId { get; set; }
        
        public decimal Value { get; set; } // 體溫 (C)
        
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
