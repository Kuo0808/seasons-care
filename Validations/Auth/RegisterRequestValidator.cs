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
                .Matches("[A-Z]").WithMessage("密碼至少需要包含一個大寫英文字母")
                .Matches("[a-z]").WithMessage("密碼至少需要包含一個小寫英文字母");

            RuleFor(x => x.Username)
                .NotEmpty().WithMessage("使用者名稱為必填")
                .MaximumLength(50).WithMessage("使用者名稱長度不可超過 50 字元");

            RuleFor(x => x.AvatarKey)
                .NotEmpty().WithMessage("頭像為必填")
                .MaximumLength(50).WithMessage("頭像代碼長度不可超過 50 字元");
        }
    }
}
