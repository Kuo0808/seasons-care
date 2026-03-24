using FluentValidation;
using SeasonsCare.Api.DTOs.HealthRecords.Weights;

namespace SeasonsCare.Api.Validations.HealthRecords.Weights
{
    public class CreateWeightRequestValidator : AbstractValidator<CreateWeightRequest>
    {
        public CreateWeightRequestValidator()
        {
            RuleFor(x => x.Value)
                .GreaterThan(0).WithMessage("體數值必須大於 0")
                .LessThan(500).WithMessage("體重數值不合理");

            RuleFor(x => x.Notes)
                .MaximumLength(500).WithMessage("備註長度不可超過 500 個字元");
        }
    }
}
