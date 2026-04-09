using System;
using System.Linq;
using System.Linq.Expressions;
using SeasonsCare.Api.DTOs.Common;

namespace SeasonsCare.Api.Repositories
{
    /// <summary>
    /// 提供列表查詢共用的日期區間過濾邏輯，避免各模組重複實作。
    /// </summary>
    public static class DateRangeQueryExtensions
    {
        public static DateRangePaginationRequest ToDateRangeRequest(this PaginationRequest request)
        {
            return request as DateRangePaginationRequest ?? new DateRangePaginationRequest
            {
                Page = request.Page,
                PageSize = request.PageSize,
                Sort = request.Sort
            };
        }

        public static IQueryable<T> ApplyDateRange<T>(
            this IQueryable<T> query,
            PaginationRequest request,
            Expression<Func<T, DateTime>> dateSelector)
        {
            var rangeRequest = request.ToDateRangeRequest();
            var (startDateUtc, endDateExclusiveUtc) = rangeRequest.ResolveDateRange(DateTime.UtcNow);
            var parameter = dateSelector.Parameters[0];

            var rangeExpression = Expression.AndAlso(
                Expression.GreaterThanOrEqual(dateSelector.Body, Expression.Constant(startDateUtc)),
                Expression.LessThan(dateSelector.Body, Expression.Constant(endDateExclusiveUtc)));

            return query.Where(Expression.Lambda<Func<T, bool>>(rangeExpression, parameter));
        }
    }
}
