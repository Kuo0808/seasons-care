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
            Username = "tester"
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
            Password = "lowercase",
            Username = "tester"
        });

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, x => x.PropertyName == nameof(RegisterRequest.Password));
    }
}
