using System;

namespace SeasonsCare.Api.DTOs.HealthRecords.Weights
{
    public class UpdateWeightRequest : CreateWeightRequest
    {
        public DateTime UpdatedAt { get; set; }
    }
}
