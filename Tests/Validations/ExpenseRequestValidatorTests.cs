using SeasonsCare.Api.DTOs.Expenses;
using SeasonsCare.Api.Models.Enums;
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
            Amount = 100m,
            Category = "food",
            ExpenseDate = new DateTime(2026, 4, 10, 0, 0, 0, DateTimeKind.Utc),
            SplitStatus = ExpenseSplitStatus.None
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
            Amount = 0m,
            Category = "traffic",
            ExpenseDate = new DateTime(2026, 4, 10, 0, 0, 0, DateTimeKind.Utc),
            SplitStatus = ExpenseSplitStatus.Pending
        });

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, x => x.PropertyName == nameof(CreateExpenseRequest.Amount));
    }

    [Fact]
    public void CreateValidator_ReturnsError_WhenCategoryIsNotSupported()
    {
        var validator = new CreateExpenseRequestValidator();
        var result = validator.Validate(new CreateExpenseRequest
        {
            Title = "Taxi",
            Amount = 100m,
            Category = "daily",
            ExpenseDate = new DateTime(2026, 4, 10, 0, 0, 0, DateTimeKind.Utc),
            SplitStatus = ExpenseSplitStatus.None
        });

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, x => x.PropertyName == nameof(CreateExpenseRequest.Category));
    }

    [Fact]
    public void UpdateValidator_ReturnsError_WhenUpdatedAtIsMissing()
    {
        var validator = new UpdateExpenseRequestValidator();
        var result = validator.Validate(new UpdateExpenseRequest
        {
            Title = "Taxi",
            Amount = 200m,
            Category = "traffic",
            ExpenseDate = new DateTime(2026, 4, 10, 0, 0, 0, DateTimeKind.Utc),
            SplitStatus = ExpenseSplitStatus.Pending
        });

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, x => x.PropertyName == nameof(UpdateExpenseRequest.UpdatedAt));
    }
}
