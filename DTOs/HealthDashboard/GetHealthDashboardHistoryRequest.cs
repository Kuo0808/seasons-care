using System;
using System.ComponentModel.DataAnnotations;

namespace SeasonsCare.Api.DTOs.HealthDashboard
{
    /// <summary>
    /// 取得健康儀表板歷史報告時使用的分頁查詢參數。
    /// </summary>
    public class GetHealthDashboardHistoryRequest
    {
        /// <summary>
        /// 頁碼，預設為 1 (第一頁)。
        /// </summary>
        [Range(1, int.MaxValue)]
        public int Page { get; set; } = 1;

        /// <summary>
        /// 每頁筆數，預設為 10，上限為 50。
        /// </summary>
        [Range(1, 50)]
        public int PageSize { get; set; } = 10;
    }
}
