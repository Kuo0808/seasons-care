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
        public DateTimeOffset RecordDate { get; set; }
        public DateTimeOffset CreatedAt { get; set; }
        public DateTimeOffset UpdatedAt { get; set; }
        public string CreatedBy { get; set; } = string.Empty;
    }
}
