using FluentValidation;
using SeasonsCare.Api.DTOs.HealthRecords.BloodSugars;

namespace SeasonsCare.Api.Validations.HealthRecords.BloodSugars
{
    public class UpdateBloodSugarRequestValidator : AbstractValidator<UpdateBloodSugarRequest>
    {
        public UpdateBloodSugarRequestValidator()
        {
            Include(new CreateBloodSugarRequestValidator());

            RuleFor(x => x.UpdatedAt)
                .NotNull().WithMessage("缺少 updatedAt 欄位，請重新整理後再試");
        }
    }
}
