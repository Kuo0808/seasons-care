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
                .MaximumLength(100).WithMessage("標題長度不可超過 100 字");

            RuleFor(x => x.Amount)
                .GreaterThan(0).WithMessage("amount 必須大於 0");

            RuleFor(x => x.Category)
                .NotEmpty().WithMessage("category 為必填")
                .MaximumLength(50).WithMessage("category 長度不可超過 50 字")
                .Must(BeValidCategory).WithMessage("category 僅支援 medical、food、traffic、other");

            RuleFor(x => x.Notes)
                .MaximumLength(500).WithMessage("notes 長度不可超過 500 字");

            RuleFor(x => x.ExpenseDate)
                .NotEqual(default(DateTime)).WithMessage("expenseDate 為必填，且需為有效 ISO 8601 日期時間");

            RuleFor(x => x.SplitStatus)
                .IsInEnum().WithMessage("splitStatus 僅支援 pending、settled、none");

            RuleFor(x => x.UpdatedAt)
                .NotNull().WithMessage("updatedAt 為必填，用於樂觀鎖檢查");
        }

        private static bool BeValidCategory(string category)
        {
            return category is "medical" or "food" or "traffic" or "other";
        }
    }
}
