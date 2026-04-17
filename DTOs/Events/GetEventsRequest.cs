using System;

namespace SeasonsCare.Api.DTOs.Events
{
    /// <summary>
    /// 查詢區間內的事件實例（FME-2）。
    /// from / to 皆必填且 to 應大於等於 from。
    /// </summary>
    public class GetEventsRequest
    {
        public DateTime From { get; set; }
        public DateTime To { get; set; }
    }
}
