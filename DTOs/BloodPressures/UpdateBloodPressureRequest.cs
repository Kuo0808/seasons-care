using System;

namespace SeasonsCare.Api.DTOs.BloodPressures
{
    public class UpdateBloodPressureRequest : CreateBloodPressureRequest
    {
        public DateTime UpdatedAt { get; set; }
    }
}
