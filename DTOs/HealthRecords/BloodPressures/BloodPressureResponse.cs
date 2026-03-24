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
        public DateTime RecordDate { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public Guid CreatedBy { get; set; }
    }
}
