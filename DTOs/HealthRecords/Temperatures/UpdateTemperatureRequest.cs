using System;

namespace SeasonsCare.Api.DTOs.HealthRecords.Temperatures
{
    public class UpdateTemperatureRequest : CreateTemperatureRequest
    {
        public DateTime UpdatedAt { get; set; }
    }
}
