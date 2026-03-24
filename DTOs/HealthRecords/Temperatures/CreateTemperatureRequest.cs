using System;

namespace SeasonsCare.Api.DTOs.HealthRecords.Temperatures
{
    public class CreateTemperatureRequest
    {
        public decimal Value { get; set; }
        public string? Notes { get; set; }
        public DateTime? RecordDate { get; set; }
    }
}
