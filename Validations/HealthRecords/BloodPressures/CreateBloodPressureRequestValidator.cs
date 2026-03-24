using FluentValidation;
using SeasonsCare.Api.DTOs.HealthRecords.BloodPressures;

namespace SeasonsCare.Api.Validations.HealthRecords.BloodPressures
{
    public class CreateBloodPressureRequestValidator : AbstractValidator<CreateBloodPressureRequest>
    {
        public CreateBloodPressureRequestValidator()
        {
            RuleFor(x => x.Systolic)
                .GreaterThan(0).WithMessage("收縮壓必須大於 0")
                .LessThan(300).WithMessage("收縮壓數值不合理");

            RuleFor(x => x.Diastolic)
                .GreaterThan(0).WithMessage("舒張壓必須大於 0")
                .LessThan(200).WithMessage("舒張壓數值不合理");

            RuleFor(x => x)
                .Must(x => x.Systolic >= x.Diastolic)
                .WithMessage("收縮壓必須大於或等於舒張壓")
                .When(x => x.Systolic > 0 && x.Diastolic > 0);

            RuleFor(x => x.Notes)
                .MaximumLength(500).WithMessage("備註長度不可超過 500 個字元");
        }
    }
}
