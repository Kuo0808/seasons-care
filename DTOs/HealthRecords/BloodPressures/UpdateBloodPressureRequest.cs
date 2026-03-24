using System;

namespace SeasonsCare.Api.DTOs.HealthRecords.BloodPressures
{
    public class UpdateBloodPressureRequest : CreateBloodPressureRequest
    {
        public DateTime UpdatedAt { get; set; }
    }
}
