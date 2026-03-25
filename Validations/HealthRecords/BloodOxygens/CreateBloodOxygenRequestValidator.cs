using FluentValidation;
using SeasonsCare.Api.DTOs.HealthRecords.BloodOxygens;

namespace SeasonsCare.Api.Validations.HealthRecords.BloodOxygens
{
    public class CreateBloodOxygenRequestValidator : AbstractValidator<CreateBloodOxygenRequest>
    {
        public CreateBloodOxygenRequestValidator()
        {
            RuleFor(x => x.SpO2)
                .GreaterThan(0).WithMessage("血氧飽和度必須大於 0")
                .LessThanOrEqualTo(100).WithMessage("血氧飽和度最高不得超過 100%");

            RuleFor(x => x.Notes)
                .MaximumLength(500).WithMessage("備註長度不可超過 500 個字元");
        }
    }
}
