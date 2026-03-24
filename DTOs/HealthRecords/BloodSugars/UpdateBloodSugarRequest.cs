using System;

namespace SeasonsCare.Api.DTOs.HealthRecords.BloodSugars
{
    public class UpdateBloodSugarRequest : CreateBloodSugarRequest
    {
        public DateTime UpdatedAt { get; set; }
    }
}
