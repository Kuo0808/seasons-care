using SeasonsCare.Api.DTOs.Common;
using SeasonsCare.Api.Validations.Common;

namespace SeasonsCare.Api.Tests.Validations;

public class DateRangePaginationRequestValidatorTests
{
    [Fact]
    public void Validator_ReturnsError_WhenStartDateIsAfterEndDate()
    {
        var validator = new DateRangePaginationRequestValidator();
        var result = validator.Validate(new DateRangePaginationRequest
        {
            StartDate = new DateTime(2026, 4, 10),
            EndDate = new DateTime(2026, 4, 9)
        });

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, x => x.ErrorMessage == "startDate 不可晚於 endDate。");
    }

    [Fact]
    public void Validator_ReturnsError_WhenPageSizeExceedsLimit()
    {
        var validator = new DateRangePaginationRequestValidator();
        var result = validator.Validate(new DateRangePaginationRequest
        {
            PageSize = 500
        });

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, x => x.PropertyName == nameof(DateRangePaginationRequest.PageSize));
    }
}
