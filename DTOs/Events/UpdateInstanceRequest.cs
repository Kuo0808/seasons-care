using System;
using System.Collections.Generic;

namespace SeasonsCare.Api.DTOs.Events
{
    /// <summary>
    /// 編輯單次事件實例（FME-5）。
    /// </summary>
    public class UpdateInstanceRequest
    {
        public DateTimeOffset ScheduledAt { get; set; }

        /// <summary>completed / cancelled / pending。</summary>
        public string? Status { get; set; }

        public string? Description { get; set; }
        public List<string>? Participants { get; set; }
    }
}
