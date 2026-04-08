using SeasonsCare.Api.DTOs.CareLogs;
using SeasonsCare.Api.Validations.CareLogs;

namespace SeasonsCare.Api.Tests.Validations;

public class CareLogRequestValidatorTests
{
    [Fact]
    public void CreateValidator_ReturnsError_WhenTitleIsMissing()
    {
        var validator = new CreateCareLogRequestValidator();
        var result = validator.Validate(new CreateCareLogRequest
        {
            Title = string.Empty
        });

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, x => x.PropertyName == nameof(CreateCareLogRequest.Title));
    }

    [Fact]
    public void UpdateValidator_ReturnsError_WhenUpdatedAtIsMissing()
    {
        var validator = new UpdateCareLogRequestValidator();
        var result = validator.Validate(new UpdateCareLogRequest
        {
            Title = "Daily note"
        });

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, x => x.PropertyName == nameof(UpdateCareLogRequest.UpdatedAt));
    }

    [Fact]
    public void CreateValidator_ReturnsError_WhenParticipantsContainsEmptyValue()
    {
        var validator = new CreateCareLogRequestValidator();
        var result = validator.Validate(new CreateCareLogRequest
        {
            Title = "Daily note",
            Participants = new List<string> { "mom", string.Empty }
        });

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, x => x.PropertyName == "Participants[1]");
    }
}
