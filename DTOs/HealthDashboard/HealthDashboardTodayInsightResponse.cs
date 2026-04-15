using System;
using System.Collections.Generic;

namespace SeasonsCare.Api.DTOs.HealthDashboard
{
    /// <summary>
    /// 今日健康摘要回應。
    /// 即時統計今日各項健康指標的量測狀態，不依賴 AI。
    /// </summary>
    public class HealthDashboardTodayInsightResponse
    {
        /// <summary>
        /// 今日是否已有健康量測紀錄。
        /// </summary>
        public bool HasTodayRecords { get; set; }

        /// <summary>
        /// 今日所有健康指標累計的紀錄筆數。
        /// </summary>
        public int RecordCount { get; set; }

        /// <summary>
        /// 今日最新一筆紀錄時間，已轉為台灣時區。若無紀錄則為 null。
        /// </summary>
        public DateTimeOffset? LatestRecordAt { get; set; }

        /// <summary>
        /// 今日摘要卡片資料，包含各指標最新值與任務進度。
        /// </summary>
        public List<HealthDashboardTodayCardDto> Cards { get; set; } = new();

        /// <summary>
        /// 回應的中繼資訊，例如信心等級與版本資訊。
        /// </summary>
        public HealthDashboardMetaDto Meta { get; set; } = new();
    }
}
