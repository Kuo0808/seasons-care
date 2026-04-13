using System;

namespace SeasonsCare.Api.DTOs.HealthRecords.Temperatures
{
    public class CreateTemperatureRequest
    {
        /// <summary>
        /// 必填。體溫數值。
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
