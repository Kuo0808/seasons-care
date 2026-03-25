using SeasonsCare.Api.DTOs.HealthRecords.BloodOxygens;
using SeasonsCare.Api.Validations.HealthRecords.BloodOxygens;

namespace SeasonsCare.Api.Tests.Validations;

public class BloodOxygenRequestValidatorTests
{
    [Fact]
    public void CreateValidator_ReturnsError_WhenSpO2IsOver100()
    {
        var validator = new CreateBloodOxygenRequestValidator();
        var result = validator.Validate(new CreateBloodOxygenRequest
        {
            SpO2 = 101m
        });

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, x => x.PropertyName == nameof(CreateBloodOxygenRequest.SpO2));
    }

    [Fact]
    public void UpdateValidator_ReturnsError_WhenUpdatedAtIsMissing()
    {
        var validator = new UpdateBloodOxygenRequestValidator();
        var result = validator.Validate(new UpdateBloodOxygenRequest
        {
            SpO2 = 98m
        });

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, x => x.PropertyName == nameof(UpdateBloodOxygenRequest.UpdatedAt));
    }
}
