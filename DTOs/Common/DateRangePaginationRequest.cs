using System;

namespace SeasonsCare.Api.DTOs.Common
{
    /// <summary>
    /// 提供列表查詢共用的日期區間、分頁與排序條件。
    /// </summary>
    public class DateRangePaginationRequest : PaginationRequest
    {
        public DateTime? StartDate { get; set; }

        public DateTime? EndDate { get; set; }

        /// <summary>
        /// 將使用者輸入的日期區間正規化為 UTC 邊界。
        /// 若未指定日期，預設回傳最近 30 天資料。
        /// </summary>
        public (DateTime StartDateUtc, DateTime EndDateExclusiveUtc) ResolveDateRange(DateTime utcNow)
        {
            var effectiveEndDate = (EndDate ?? utcNow).Date;
            var effectiveStartDate = (StartDate ?? effectiveEndDate.AddDays(-30)).Date;

            return (
                DateTime.SpecifyKind(effectiveStartDate, DateTimeKind.Utc),
                DateTime.SpecifyKind(effectiveEndDate.AddDays(1), DateTimeKind.Utc));
        }
    }
}
