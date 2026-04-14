using System;

namespace SeasonsCare.Api.DTOs.HealthRecords.Weights
{
    public class WeightResponse
    {
        public Guid Id { get; set; }
        public Guid CareGroupId { get; set; }
        public decimal Value { get; set; }
        public string? Notes { get; set; }
        public DateTimeOffset RecordDate { get; set; }
        public DateTimeOffset CreatedAt { get; set; }
        public DateTimeOffset UpdatedAt { get; set; }
        public string CreatedBy { get; set; } = string.Empty;
    }
}
