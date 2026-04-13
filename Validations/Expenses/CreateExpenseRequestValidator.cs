using FluentValidation;
using SeasonsCare.Api.DTOs.Expenses;

namespace SeasonsCare.Api.Validations.Expenses
{
    public class CreateExpenseRequestValidator : AbstractValidator<CreateExpenseRequest>
    {
        public CreateExpenseRequestValidator()
        {
            RuleFor(x => x.Title)
                .NotEmpty().WithMessage("璅??箏?憛?")
                .MaximumLength(100).WithMessage("璅??瑕漲銝頞? 100 摮?");

            RuleFor(x => x.Amount)
                .GreaterThan(0).WithMessage("amount 敹?憭扳 0");

            RuleFor(x => x.Category)
                .NotEmpty().WithMessage("category ?箏?憛?")
                .MaximumLength(50).WithMessage("category ?瑕漲銝頞? 50 摮?")
                .Must(BeValidCategory).WithMessage("category ???medical?ood?raffic?ther");

            RuleFor(x => x.Notes)
                .MaximumLength(500).WithMessage("notes ?瑕漲銝頞? 500 摮?");

            RuleFor(x => x.ExpenseDate)
                .NotEqual(default(DateTime)).WithMessage("expenseDate ?箏?憛恬?銝??箸???ISO 8601 ?交???");

            RuleFor(x => x.SplitStatus)
                .IsInEnum().WithMessage("splitStatus ???pending?ettled?one");
        }

        private static bool BeValidCategory(string category)
        {
            return category is "medical" or "food" or "traffic" or "other";
        }
    }
}
