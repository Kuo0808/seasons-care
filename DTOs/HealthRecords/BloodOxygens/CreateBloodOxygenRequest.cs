using System;

namespace SeasonsCare.Api.DTOs.HealthRecords.BloodOxygens
{
    public class CreateBloodOxygenRequest
    {
        /// <summary>
        /// 必填。血氧飽和度，需大於 0。
        /// </summary>
        public decimal SpO2 { get; set; }

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
