using System;

namespace SeasonsCare.Api.DTOs.HealthDashboard
{
    public class SingleValueTrendPointDto
    {
        public DateTime RecordDate { get; set; }

        public decimal Value { get; set; }

        public string? Notes { get; set; }
    }
}
