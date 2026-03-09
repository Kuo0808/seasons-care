using FluentValidation;
using SeasonsCare.Api.DTOs.Auth;

namespace SeasonsCare.Api.Validations.Auth
{
    public class RegisterRequestValidator : AbstractValidator<RegisterRequest>
    {
        public RegisterRequestValidator()
        {
            RuleFor(x => x.Email)
                .NotEmpty().WithMessage("請輸入 Email")
                .EmailAddress().WithMessage("Email 格式不正確");

            RuleFor(x => x.Password)
                .NotEmpty().WithMessage("請輸入密碼")
                .MinimumLength(6).WithMessage("密碼長度至少需 6 碼")
                .Matches("[A-Z]").WithMessage("密碼必須包含至少一個大寫英文字母")
                .Matches("[a-z]").WithMessage("密碼必須包含至少一個小寫英文字母");

            RuleFor(x => x.Username)
                .NotEmpty().WithMessage("請輸入使用者名稱")
                .MaximumLength(50).WithMessage("使用者名稱不可超過 50 個字元");
        }
    }
}
