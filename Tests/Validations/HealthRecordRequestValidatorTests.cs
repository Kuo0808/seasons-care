using SeasonsCare.Api.DTOs.HealthRecords.BloodPressures;
using SeasonsCare.Api.DTOs.HealthRecords.BloodSugars;
using SeasonsCare.Api.Validations.HealthRecords.BloodPressures;
using SeasonsCare.Api.Validations.HealthRecords.BloodSugars;

namespace SeasonsCare.Api.Tests.Validations;

public class HealthRecordRequestValidatorTests
{
    [Fact]
    public void BloodSugarCreateValidator_ReturnsError_WhenMeasurementContextIsMissing()
    {
        var validator = new CreateBloodSugarRequestValidator();
        var result = validator.Validate(new CreateBloodSugarRequest
        {
            GlucoseLevel = 120m,
            MeasurementContext = string.Empty
        });

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, x => x.PropertyName == nameof(CreateBloodSugarRequest.MeasurementContext));
    }

    [Fact]
    public void BloodSugarUpdateValidator_ReturnsError_WhenUpdatedAtIsMissing()
    {
        var validator = new UpdateBloodSugarRequestValidator();
        var result = validator.Validate(new UpdateBloodSugarRequest
        {
            GlucoseLevel = 120m,
            MeasurementContext = "飯前"
        });

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, x => x.PropertyName == nameof(UpdateBloodSugarRequest.UpdatedAt));
    }

    [Fact]
    public void BloodPressureCreateValidator_ReturnsError_WhenSystolicIsLowerThanDiastolic()
    {
        var validator = new CreateBloodPressureRequestValidator();
        var result = validator.Validate(new CreateBloodPressureRequest
        {
            Systolic = 70,
            Diastolic = 90
        });

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, x => x.ErrorMessage.Contains("收縮壓必須大於或等於舒張壓"));
    }

    [Fact]
    public void BloodPressureUpdateValidator_ReturnsError_WhenUpdatedAtIsMissing()
    {
        var validator = new UpdateBloodPressureRequestValidator();
        var result = validator.Validate(new UpdateBloodPressureRequest
        {
            Systolic = 120,
            Diastolic = 80
        });

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, x => x.PropertyName == nameof(UpdateBloodPressureRequest.UpdatedAt));
    }
}
