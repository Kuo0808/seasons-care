using SeasonsCare.Api.DTOs.CareGroups;
using SeasonsCare.Api.Validations.CareGroups;

namespace SeasonsCare.Api.Tests.Validations;

public class CareGroupRequestValidatorTests
{
    [Fact]
    public void CreateValidator_ReturnsError_WhenRecipientGenderIsMissing()
    {
        var validator = new CreateCareGroupRequestValidator();
        var result = validator.Validate(new CreateCareGroupRequest
        {
            RecipientName = "Dad",
            RecipientGender = string.Empty,
            RecipientBirthDate = new DateOnly(1950, 1, 2)
        });

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, x => x.PropertyName == nameof(CreateCareGroupRequest.RecipientGender));
    }

    [Fact]
    public void UpdateValidator_ReturnsError_WhenRecipientNameIsMissing()
    {
        var validator = new UpdateCareGroupRequestValidator();
        var result = validator.Validate(new UpdateCareGroupRequest
        {
            Name = "Home Care",
            RecipientName = string.Empty,
            RecipientGender = "Male",
            RecipientBirthDate = new DateOnly(1950, 1, 2)
        });

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, x => x.PropertyName == nameof(UpdateCareGroupRequest.RecipientName));
    }
}
