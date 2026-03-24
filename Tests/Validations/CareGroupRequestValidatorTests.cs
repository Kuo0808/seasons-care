using SeasonsCare.Api.DTOs.CareGroups;
using SeasonsCare.Api.Validations.CareGroups;

namespace SeasonsCare.Api.Tests.Validations;

public class CareGroupRequestValidatorTests
{
    [Fact]
    public void CreateValidator_ReturnsError_WhenNameIsMissing()
    {
        var validator = new CreateCareGroupRequestValidator();
        var result = validator.Validate(new CreateCareGroupRequest
        {
            Name = string.Empty,
            RecipientName = "Dad"
        });

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, x => x.PropertyName == nameof(CreateCareGroupRequest.Name));
    }

    [Fact]
    public void UpdateValidator_ReturnsError_WhenRecipientNameIsMissing()
    {
        var validator = new UpdateCareGroupRequestValidator();
        var result = validator.Validate(new UpdateCareGroupRequest
        {
            Name = "Home Care",
            RecipientName = string.Empty
        });

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, x => x.PropertyName == nameof(UpdateCareGroupRequest.RecipientName));
    }
}
