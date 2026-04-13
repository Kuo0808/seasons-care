using System;

namespace SeasonsCare.Api.DTOs.HealthDashboard
{
    public class BloodSugarTrendPointDto
    {
        public DateTime RecordDate { get; set; }

        public decimal Value { get; set; }

        public string MeasurementContext { get; set; } = string.Empty;

        public string? Notes { get; set; }
    }
}
