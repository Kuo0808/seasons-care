using System;

namespace SeasonsCare.Api.DTOs.HealthRecords.BloodOxygens
{
    public class CreateBloodOxygenRequest
    {
        public decimal SpO2 { get; set; }
        public string? Notes { get; set; }
        public DateTime? RecordDate { get; set; }
    }
}
