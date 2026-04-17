using System;
using System.Collections.Generic;

namespace SeasonsCare.Api.DTOs.Events
{
    /// <summary>
    /// 編輯單次事件實例（FME-5）。
    /// scheduledAt 指定要改的是系列中的哪一次；其餘欄位皆為選填，只覆寫有提供的內容。
    /// </summary>
    public class UpdateInstanceRequest
    {
        /// <summary>目標實例時間（系列展開時的 scheduledStartAt）。</summary>
        public DateTime ScheduledAt { get; set; }

        /// <summary>
        /// 目標狀態：
        /// - completed：標記完成
        /// - cancelled：標記取消
        /// - pending：清除 override 回復系列預設（若同時提供 description/participants，會視為更新 override 並保留 Scheduled 狀態）
        /// </summary>
        public string? Status { get; set; }

        /// <summary>單次覆寫描述。</summary>
        public string? Description { get; set; }

        /// <summary>單次覆寫參與者 userId 清單。</summary>
        public List<string>? Participants { get; set; }
    }
}
