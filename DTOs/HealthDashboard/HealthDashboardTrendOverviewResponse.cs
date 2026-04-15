using System;
using System.Collections.Generic;

namespace SeasonsCare.Api.DTOs.HealthDashboard
{
    /// <summary>
    /// 近七天健康趨勢總覽回應。
    /// 即時統計各健康指標的趨勢卡片與折線圖資料，不依賴 AI。
    /// 趨勢狀態標籤優先使用已快取的 AI 結果，無快取時由規則判斷。
    /// </summary>
    public class HealthDashboardTrendOverviewResponse
    {
        /// <summary>
        /// 趨勢統計區間起始時間，已轉為台灣時區。
        /// </summary>
        public DateTimeOffset DateFrom { get; set; }

        /// <summary>
        /// 趨勢統計區間結束時間，已轉為台灣時區。
        /// </summary>
        public DateTimeOffset DateTo { get; set; }

        /// <summary>
        /// 各健康指標的趨勢卡片資料，包含平均值、最新值、變化量與每日圖表點位。
        /// </summary>
        public List<HealthDashboardTrendCardResponse> Metrics { get; set; } = new();
    }
}
