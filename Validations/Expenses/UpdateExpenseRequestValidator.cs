using FluentValidation;
using SeasonsCare.Api.DTOs.Expenses;

namespace SeasonsCare.Api.Validations.Expenses
{
    public class UpdateExpenseRequestValidator : AbstractValidator<UpdateExpenseRequest>
    {
        public UpdateExpenseRequestValidator()
        {
            RuleFor(x => x.Title)
                .NotEmpty().WithMessage("標題為必填")
                .MaximumLength(100).WithMessage("標題長度不能超過100字");

            RuleFor(x => x.Amount)
                .GreaterThan(0).WithMessage("金額必須大於0");
                
            RuleFor(x => x.Category)
                .MaximumLength(50).WithMessage("類別長度不能超過50字");

            RuleFor(x => x.UpdatedAt)
                .NotNull().WithMessage("缺少 updatedAt 欄位，請重新整理後再試");
        }
    }
}
