using System;

namespace SeasonsCare.Api.DTOs.Events
{
    /// <summary>
    /// 查詢區間內的事件實例（FME-2）。
    /// </summary>
    public class GetEventsRequest
    {
        public DateTimeOffset From { get; set; }
        public DateTimeOffset To { get; set; }
    }
}
