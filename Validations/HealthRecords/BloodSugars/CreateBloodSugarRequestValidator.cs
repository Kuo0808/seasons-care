using FluentValidation;
using SeasonsCare.Api.DTOs.HealthRecords.BloodSugars;

namespace SeasonsCare.Api.Validations.HealthRecords.BloodSugars
{
    public class CreateBloodSugarRequestValidator : AbstractValidator<CreateBloodSugarRequest>
    {
        public CreateBloodSugarRequestValidator()
        {
            RuleFor(x => x.GlucoseLevel)
                .GreaterThan(0).WithMessage("血糖值必須大於 0")
                .LessThan(1000).WithMessage("血糖數值不合理");

            RuleFor(x => x.MeasurementContext)
                .NotEmpty().WithMessage("請提供量測情境 (例如: 飯前, 飯後)")
                .MaximumLength(50).WithMessage("量測情境長度不可超過 50 個字元");

            RuleFor(x => x.Notes)
                .MaximumLength(500).WithMessage("備註長度不可超過 500 個字元");
        }
    }
}
