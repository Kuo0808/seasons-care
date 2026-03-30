using FluentValidation;
using SeasonsCare.Api.DTOs.HealthRecords.BloodOxygens;

namespace SeasonsCare.Api.Validations.HealthRecords.BloodOxygens
{
    public class UpdateBloodOxygenRequestValidator : AbstractValidator<UpdateBloodOxygenRequest>
    {
        public UpdateBloodOxygenRequestValidator()
        {
            Include(new CreateBloodOxygenRequestValidator());

            RuleFor(x => x.UpdatedAt)
                .NotEqual(default(DateTime)).WithMessage("缺少 updatedAt 欄位，請重新整理後再試");
        }
    }
}
