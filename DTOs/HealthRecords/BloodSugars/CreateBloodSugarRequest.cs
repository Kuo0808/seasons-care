using System;

namespace SeasonsCare.Api.DTOs.HealthRecords.BloodSugars
{
    public class CreateBloodSugarRequest
    {
        public decimal GlucoseLevel { get; set; }
        public string MeasurementContext { get; set; } = string.Empty;
        public string? Notes { get; set; }
        public DateTime? RecordDate { get; set; }
    }
}
