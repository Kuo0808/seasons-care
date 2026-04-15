namespace SeasonsCare.Api.DTOs.HealthDashboard
{
    /// <summary>
    /// 描述 fallback 狀態、可信度與版本資訊的中繼資料。
    /// </summary>
    public class HealthDashboardMetaDto
    {
        /// <summary>
        /// 表示目前回傳的是 fallback 內容，而非 AI 正式生成內容。
        /// </summary>
        public bool IsFallback { get; set; }

        /// <summary>
        /// 儀表板內容的可信度等級，例如 low、medium、high。
        /// </summary>
        public string Confidence { get; set; } = "low";

        /// <summary>
        /// 若有 AI 生成內容，這裡記錄使用的模型名稱。
        /// </summary>
        public string? ModelName { get; set; }

        /// <summary>
        /// 若有 AI 生成內容，這裡記錄使用的 prompt 版本。
        /// </summary>
        public string? PromptVersion { get; set; }

        /// <summary>
        /// 代表目前 API 回應欄位規格的 rules 版本。
        /// </summary>
        public string? RulesVersion { get; set; }
    }
}
