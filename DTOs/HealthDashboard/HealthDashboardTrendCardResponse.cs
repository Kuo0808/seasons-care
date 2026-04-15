using System.Collections.Generic;

namespace SeasonsCare.Api.DTOs.HealthDashboard
{
    /// <summary>
    /// 單一健康指標的趨勢卡片資料。
    /// </summary>
    public class HealthDashboardTrendCardResponse
    {
        /// <summary>
        /// 指標代碼，例如 blood_pressure 或 blood_sugar。
        /// </summary>
        public string MetricType { get; set; } = string.Empty;

        /// <summary>
        /// 指標顯示名稱。
        /// </summary>
        public string Title { get; set; } = string.Empty;

        /// <summary>
        /// 趨勢狀態標籤，供 UI 顯示 badge。
        /// </summary>
        public string StatusLabel { get; set; } = string.Empty;

        /// <summary>
        /// 趨勢狀態代碼，供 UI 邏輯判斷。
        /// </summary>
        public string Status { get; set; } = string.Empty;

        /// <summary>
        /// 卡片摘要說明文字。
        /// </summary>
        public string Summary { get; set; } = string.Empty;

        /// <summary>
        /// 主要顯示值，通常為近七天平均值。
        /// </summary>
        public string DisplayValue { get; set; } = string.Empty;

        /// <summary>
        /// 最新一筆量測值的格式化文字。
        /// </summary>
        public string LatestValue { get; set; } = string.Empty;

        /// <summary>
        /// 起始點與最新點之間的變化量。
        /// </summary>
        public string Change { get; set; } = string.Empty;

        /// <summary>
        /// 簡化後的趨勢方向，例如 up、down 或 steady。
        /// </summary>
        public string TrendDirection { get; set; } = string.Empty;

        /// <summary>
        /// 數值單位，例如 mmHg 或 mg/dL。
        /// </summary>
        public string Unit { get; set; } = string.Empty;

        /// <summary>
        /// 主要平均值的原始數值。
        /// </summary>
        public decimal? AverageValue { get; set; }

        /// <summary>
        /// 次要平均值的原始數值，主要用於雙軸指標，例如血壓舒張壓。
        /// </summary>
        public decimal? SecondaryAverageValue { get; set; }

        /// <summary>
        /// 主要數值的每日圖表點位資料。
        /// </summary>
        public List<HealthDashboardTrendPointResponse> Points { get; set; } = new();

        /// <summary>
        /// 次要數值的每日圖表點位資料。
        /// </summary>
        public List<HealthDashboardTrendPointResponse>? SecondaryPoints { get; set; }
    }
}
