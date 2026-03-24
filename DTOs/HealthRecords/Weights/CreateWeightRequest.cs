using System;

namespace SeasonsCare.Api.DTOs.HealthRecords.Weights
{
    public class CreateWeightRequest
    {
        public decimal Value { get; set; }
        public string? Notes { get; set; }
        public DateTime? RecordDate { get; set; }
    }
}
