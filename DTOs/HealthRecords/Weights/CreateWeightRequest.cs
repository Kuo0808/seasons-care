using System;

namespace SeasonsCare.Api.DTOs.HealthRecords.Weights
{
    public class CreateWeightRequest
    {
        /// <summary>
        /// 必填。體重數值，需大於 0。
        /// </summary>
        public decimal Value { get; set; }

        /// <summary>
        /// 選填。備註，最長 500 字。
        /// </summary>
        public string? Notes { get; set; }

        /// <summary>
        /// 選填。量測時間；省略時由後端使用目前 UTC 時間。
        /// </summary>
        public DateTime? RecordDate { get; set; }
    }
}
