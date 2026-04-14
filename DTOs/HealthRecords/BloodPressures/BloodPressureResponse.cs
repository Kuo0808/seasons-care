using System;

namespace SeasonsCare.Api.DTOs.HealthRecords.BloodPressures
{
    public class BloodPressureResponse
    {
        public Guid Id { get; set; }
        public Guid CareGroupId { get; set; }
        public int Systolic { get; set; }
        public int Diastolic { get; set; }
        public string? Notes { get; set; }
        public DateTimeOffset RecordDate { get; set; }
        public DateTimeOffset CreatedAt { get; set; }
        public DateTimeOffset UpdatedAt { get; set; }
        public string CreatedBy { get; set; } = string.Empty;
    }
}
