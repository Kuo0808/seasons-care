using SeasonsCare.Api.DTOs.Auth;
using SeasonsCare.Api.Validations.Auth;

namespace SeasonsCare.Api.Tests.Validations;

public class RegisterRequestValidatorTests
{
    [Fact]
    public void RegisterValidator_ReturnsError_WhenEmailIsInvalid()
    {
        var validator = new RegisterRequestValidator();
        var result = validator.Validate(new RegisterRequest
        {
            Email = "invalid-email",
            Password = "Password1"
        });

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, x => x.PropertyName == nameof(RegisterRequest.Email));
    }

    [Fact]
    public void RegisterValidator_ReturnsError_WhenPasswordIsTooShort()
    {
        var validator = new RegisterRequestValidator();
        var result = validator.Validate(new RegisterRequest
        {
            Email = "tester@example.com",
            Password = "abc"
        });

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, x => x.PropertyName == nameof(RegisterRequest.Password));
    }

    [Fact]
    public void CompleteProfileValidator_ReturnsError_WhenAvatarKeyIsMissing()
    {
        var validator = new CompleteProfileRequestValidator();
        var result = validator.Validate(new CompleteProfileRequest
        {
            Username = "tester",
            AvatarKey = string.Empty
        });

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, x => x.PropertyName == nameof(CompleteProfileRequest.AvatarKey));
    }
}
