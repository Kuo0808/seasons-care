using FluentValidation;
using SeasonsCare.Api.DTOs.HealthRecords.Weights;

namespace SeasonsCare.Api.Validations.HealthRecords.Weights
{
    public class UpdateWeightRequestValidator : AbstractValidator<UpdateWeightRequest>
    {
        public UpdateWeightRequestValidator()
        {
            Include(new CreateWeightRequestValidator());

            RuleFor(x => x.UpdatedAt)
                .NotNull().WithMessage("缺少 updatedAt 欄位，請重新整理後再試");
        }
    }
}
