using System;

namespace SeasonsCare.Api.DTOs.HealthRecords.BloodPressures
{
    public class CreateBloodPressureRequest
    {
        /// <summary>
        /// 必填。收縮壓，需大於 0。
        /// </summary>
        public int Systolic { get; set; }

        /// <summary>
        /// 必填。舒張壓，需大於 0。
        /// </summary>
        public int Diastolic { get; set; }

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
