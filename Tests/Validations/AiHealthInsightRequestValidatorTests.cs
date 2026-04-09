using SeasonsCare.Api.DTOs.AiHealthInsights;
using SeasonsCare.Api.Validations.AiHealthInsights;

namespace SeasonsCare.Api.Tests.Validations;

public class AiHealthInsightRequestValidatorTests
{
    [Fact]
    public void Validator_ReturnsError_WhenReportTypeIsMissing()
    {
        var validator = new SaveAiHealthInsightRequestValidator();
        var result = validator.Validate(new SaveAiHealthInsightRequest
        {
            DateFrom = new DateTime(2026, 4, 1),
            DateTo = new DateTime(2026, 4, 2),
            OverallSummary = "summary",
            KeyInsights = "insights",
            Recommendations = "recommendations"
        });

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, x => x.PropertyName == nameof(SaveAiHealthInsightRequest.ReportType));
    }

    [Fact]
    public void Validator_ReturnsError_WhenDateFromIsAfterDateTo()
    {
        var validator = new SaveAiHealthInsightRequestValidator();
        var result = validator.Validate(new SaveAiHealthInsightRequest
        {
            ReportType = "daily",
            DateFrom = new DateTime(2026, 4, 2),
            DateTo = new DateTime(2026, 4, 1),
            OverallSummary = "summary",
            KeyInsights = "insights",
            Recommendations = "recommendations"
        });

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, x => x.ErrorMessage == "dateFrom 不可晚於 dateTo");
    }
}
