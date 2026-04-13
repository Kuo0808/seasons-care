using System;

namespace SeasonsCare.Api.DTOs.Common
{
    public class DateRangePaginationRequest : PaginationRequest
    {
        /// <summary>
        /// 選填。查詢起始日期，為包含邊界的日期。
        /// </summary>
        public DateTime? StartDate { get; set; }

        /// <summary>
        /// 選填。查詢結束日期，為包含邊界的日期。
        /// </summary>
        public DateTime? EndDate { get; set; }

        public (DateTime StartDateUtc, DateTime EndDateExclusiveUtc) ResolveDateRange(DateTime utcNow)
        {
            if (!StartDate.HasValue && !EndDate.HasValue)
            {
                var defaultAnchorDate = utcNow.Date;
                return (
                    DateTime.SpecifyKind(defaultAnchorDate.AddDays(-60), DateTimeKind.Utc),
                    DateTime.SpecifyKind(defaultAnchorDate.AddDays(61), DateTimeKind.Utc));
            }

            var effectiveEndDate = (EndDate ?? utcNow).Date;
            var effectiveStartDate = (StartDate ?? effectiveEndDate.AddDays(-30)).Date;

            return (
                DateTime.SpecifyKind(effectiveStartDate, DateTimeKind.Utc),
                DateTime.SpecifyKind(effectiveEndDate.AddDays(1), DateTimeKind.Utc));
        }
    }
}
