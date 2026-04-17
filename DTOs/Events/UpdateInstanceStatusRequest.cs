using System;

namespace SeasonsCare.Api.DTOs.Events
{
    /// <summary>
    /// 編輯單次事件狀態（FME-5）。
    /// </summary>
    public class UpdateInstanceStatusRequest
    {
        /// <summary>目標實例時間（系列展開時的 scheduledStartAt）。</summary>
        public DateTime ScheduledAt { get; set; }

        /// <summary>
        /// 目標狀態：
        /// - completed：標記完成（建立或更新 override）
        /// - cancelled：標記取消（建立或更新 override）
        /// - pending：清除 override，回復為系列預設
        /// </summary>
        public string Status { get; set; } = string.Empty;
    }
}
