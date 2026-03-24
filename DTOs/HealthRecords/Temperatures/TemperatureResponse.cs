using System;

namespace SeasonsCare.Api.DTOs.HealthRecords.Temperatures
{
    public class TemperatureResponse
    {
        public Guid Id { get; set; }
        public Guid CareGroupId { get; set; }
        public decimal Value { get; set; }
        public string? Notes { get; set; }
        public DateTime RecordDate { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public string CreatedBy { get; set; } = string.Empty;
    }
}
