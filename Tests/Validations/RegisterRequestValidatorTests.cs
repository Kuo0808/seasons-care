using SeasonsCare.Api.DTOs.Auth;
using SeasonsCare.Api.Validations.Auth;

namespace SeasonsCare.Api.Tests.Validations;

public class RegisterRequestValidatorTests
{
    [Fact]
    public void Validator_ReturnsError_WhenEmailIsInvalid()
    {
        var validator = new RegisterRequestValidator();
        var result = validator.Validate(new RegisterRequest
        {
            Email = "invalid-email",
            Password = "Password1",
            Username = "tester",
            AvatarKey = "dog_01"
        });

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, x => x.PropertyName == nameof(RegisterRequest.Email));
    }

    [Fact]
    public void Validator_ReturnsError_WhenPasswordIsTooWeak()
    {
        var validator = new RegisterRequestValidator();
        var result = validator.Validate(new RegisterRequest
        {
            Email = "tester@example.com",
            Password = "abc",
            Username = "tester",
            AvatarKey = "dog_01"
        });

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, x => x.PropertyName == nameof(RegisterRequest.Password));
    }

    [Fact]
    public void Validator_ReturnsError_WhenPasswordIsTooLong()
    {
        var validator = new RegisterRequestValidator();
        var result = validator.Validate(new RegisterRequest
        {
            Email = "tester@example.com",
            Password = "abcdefghijklmn",
            Username = "tester",
            AvatarKey = "dog_01"
        });

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, x => x.PropertyName == nameof(RegisterRequest.Password));
    }
}
