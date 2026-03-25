using System;

namespace SeasonsCare.Api.DTOs.HealthRecords.BloodOxygens
{
    public class UpdateBloodOxygenRequest : CreateBloodOxygenRequest
    {
        public DateTime UpdatedAt { get; set; }
    }
}
