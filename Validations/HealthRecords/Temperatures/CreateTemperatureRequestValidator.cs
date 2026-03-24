using FluentValidation;
using SeasonsCare.Api.DTOs.HealthRecords.Temperatures;

namespace SeasonsCare.Api.Validations.HealthRecords.Temperatures
{
    public class CreateTemperatureRequestValidator : AbstractValidator<CreateTemperatureRequest>
    {
        public CreateTemperatureRequestValidator()
        {
            RuleFor(x => x.Value)
                .GreaterThan(20).WithMessage("體溫數值必須大於 20 度")
                .LessThan(45).WithMessage("體溫數值不合理，請確認是否輸入正確");

            RuleFor(x => x.Notes)
                .MaximumLength(500).WithMessage("備註長度不可超過 500 個字元");
        }
    }
}
