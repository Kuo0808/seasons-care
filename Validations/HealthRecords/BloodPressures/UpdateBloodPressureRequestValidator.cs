using FluentValidation;
using SeasonsCare.Api.DTOs.HealthRecords.BloodPressures;

namespace SeasonsCare.Api.Validations.HealthRecords.BloodPressures
{
    public class UpdateBloodPressureRequestValidator : AbstractValidator<UpdateBloodPressureRequest>
    {
        public UpdateBloodPressureRequestValidator()
        {
            Include(new CreateBloodPressureRequestValidator());

            RuleFor(x => x.UpdatedAt)
                .NotNull().WithMessage("缺少 updatedAt 欄位，請重新整理後再試");
        }
    }
}
