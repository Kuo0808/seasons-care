using System;

namespace SeasonsCare.Api.DTOs.HealthRecords.BloodSugars
{
    public class CreateBloodSugarRequest
    {
        /// <summary>
        /// 必填。血糖值，需大於 0。
        /// </summary>
        public decimal GlucoseLevel { get; set; }

        /// <summary>
        /// 必填。量測情境，例如飯前或飯後，最長 50 字。
        /// </summary>
        public string MeasurementContext { get; set; } = string.Empty;

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
