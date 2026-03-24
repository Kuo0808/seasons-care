using System;

namespace SeasonsCare.Api.DTOs.BloodPressures
{
    public class CreateBloodPressureRequest
    {
        public int Systolic { get; set; }
        public int Diastolic { get; set; }
        public string? Notes { get; set; }
        public DateTime? RecordDate { get; set; }
    }
}
