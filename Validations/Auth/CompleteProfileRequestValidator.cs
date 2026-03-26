using FluentValidation;
using SeasonsCare.Api.DTOs.Auth;

namespace SeasonsCare.Api.Validations.Auth
{
    public class CompleteProfileRequestValidator : AbstractValidator<CompleteProfileRequest>
    {
        public CompleteProfileRequestValidator()
        {
            RuleFor(x => x.Username)
                .NotEmpty().WithMessage("使用者名稱為必填")
                .MaximumLength(50).WithMessage("使用者名稱長度不可超過 50 字元");

            RuleFor(x => x.AvatarKey)
                .NotEmpty().WithMessage("頭像為必填")
                .MaximumLength(50).WithMessage("頭像代碼長度不可超過 50 字元");
        }
    }
}
