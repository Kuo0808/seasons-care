using System;

namespace SeasonsCare.Api.DTOs.HealthDashboard
{
    public class BloodPressureTrendPointDto
    {
        public DateTime RecordDate { get; set; }

        public int Systolic { get; set; }

        public int Diastolic { get; set; }

        public string? Notes { get; set; }
    }
}
