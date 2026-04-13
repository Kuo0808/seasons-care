using System;

namespace SeasonsCare.Api.DTOs.HealthRecords.Weights
{
    public class UpdateWeightRequest : CreateWeightRequest
    {
        /// <summary>
        /// 必填。前一次查詢到的 updatedAt，用於樂觀鎖檢查。
        /// </summary>
        public DateTime UpdatedAt { get; set; }
    }
}
