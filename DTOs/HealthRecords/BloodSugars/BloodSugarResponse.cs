using System;

namespace SeasonsCare.Api.DTOs.HealthRecords.BloodSugars
{
    public class BloodSugarResponse
    {
        public Guid Id { get; set; }
        public Guid CareGroupId { get; set; }
        public decimal GlucoseLevel { get; set; }
        public string MeasurementContext { get; set; } = string.Empty;
        public string? Notes { get; set; }
        public DateTime RecordDate { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public string CreatedBy { get; set; } = string.Empty;
    }
}
