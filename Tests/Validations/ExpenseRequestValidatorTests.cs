using SeasonsCare.Api.DTOs.Expenses;
using SeasonsCare.Api.Validations.Expenses;

namespace SeasonsCare.Api.Tests.Validations;

public class ExpenseRequestValidatorTests
{
    [Fact]
    public void CreateValidator_ReturnsError_WhenTitleIsMissing()
    {
        var validator = new CreateExpenseRequestValidator();
        var result = validator.Validate(new CreateExpenseRequest
        {
            Title = string.Empty,
            Amount = 100m
        });

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, x => x.PropertyName == nameof(CreateExpenseRequest.Title));
    }

    [Fact]
    public void CreateValidator_ReturnsError_WhenAmountIsNotPositive()
    {
        var validator = new CreateExpenseRequestValidator();
        var result = validator.Validate(new CreateExpenseRequest
        {
            Title = "Taxi",
            Amount = 0m
        });

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, x => x.PropertyName == nameof(CreateExpenseRequest.Amount));
    }

    [Fact]
    public void UpdateValidator_ReturnsError_WhenUpdatedAtIsMissing()
    {
        var validator = new UpdateExpenseRequestValidator();
        var result = validator.Validate(new UpdateExpenseRequest
        {
            Title = "Taxi",
            Amount = 200m
        });

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, x => x.PropertyName == nameof(UpdateExpenseRequest.UpdatedAt));
    }
}
