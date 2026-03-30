using FluentValidation;
using SeasonsCare.Api.DTOs.HealthRecords.Temperatures;

namespace SeasonsCare.Api.Validations.HealthRecords.Temperatures
{
    public class UpdateTemperatureRequestValidator : AbstractValidator<UpdateTemperatureRequest>
    {
        public UpdateTemperatureRequestValidator()
        {
            Include(new CreateTemperatureRequestValidator());

            RuleFor(x => x.UpdatedAt)
                .NotEqual(default(DateTime)).WithMessage("缺少 updatedAt 欄位，請重新整理後再試");
        }
    }
}
