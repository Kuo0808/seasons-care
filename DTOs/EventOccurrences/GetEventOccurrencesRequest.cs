using System;

namespace SeasonsCare.Api.DTOs.EventOccurrences
{
    public class GetEventOccurrencesRequest
    {
        /// <summary>
        /// 必填。查詢區間起點，請使用 ISO 8601 UTC 格式。
        /// </summary>
        public DateTime From { get; set; }

        /// <summary>
        /// 必填。查詢區間終點，請使用 ISO 8601 UTC 格式。
        /// </summary>
        public DateTime To { get; set; }
    }
}
