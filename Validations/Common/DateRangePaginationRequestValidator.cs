using FluentValidation;
using SeasonsCare.Api.DTOs.Common;

namespace SeasonsCare.Api.Validations.Common
{
    /// <summary>
    /// 驗證共用日期區間列表查詢，確保前後端協作時的查詢格式一致。
    /// </summary>
    public class DateRangePaginationRequestValidator : AbstractValidator<DateRangePaginationRequest>
    {
        public DateRangePaginationRequestValidator()
        {
            RuleFor(x => x.Page)
                .GreaterThanOrEqualTo(1)
                .WithMessage("page 必須大於或等於 1。");

            RuleFor(x => x.PageSize)
                .InclusiveBetween(1, 200)
                .WithMessage("pageSize 必須介於 1 到 200 之間。");

            RuleFor(x => x.Sort)
                .NotEmpty()
                .WithMessage("sort 為必填。");

            RuleFor(x => x)
                .Must(x => !x.StartDate.HasValue || !x.EndDate.HasValue || x.StartDate.Value.Date <= x.EndDate.Value.Date)
                .WithMessage("startDate 不可晚於 endDate。");
        }
    }
}
