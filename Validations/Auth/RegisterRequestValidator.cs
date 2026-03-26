using FluentValidation;
using SeasonsCare.Api.DTOs.Auth;

namespace SeasonsCare.Api.Validations.Auth
{
    public class RegisterRequestValidator : AbstractValidator<RegisterRequest>
    {
        public RegisterRequestValidator()
        {
            RuleFor(x => x.Email)
                .NotEmpty().WithMessage("Email 為必填")
                .EmailAddress().WithMessage("Email 格式不正確");

            RuleFor(x => x.Password)
                .NotEmpty().WithMessage("密碼為必填")
                .MinimumLength(6).WithMessage("密碼長度至少需要 6 碼")
                .MaximumLength(12).WithMessage("密碼長度不可超過 12 碼");

            RuleFor(x => x.Username)
                .NotEmpty().WithMessage("使用者名稱為必填")
                .MaximumLength(50).WithMessage("使用者名稱長度不可超過 50 字元");

            RuleFor(x => x.AvatarKey)
                .NotEmpty().WithMessage("頭像為必填")
                .MaximumLength(50).WithMessage("頭像代碼長度不可超過 50 字元");
        }
    }
}
