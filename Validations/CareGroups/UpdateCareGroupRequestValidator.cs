using FluentValidation;
using SeasonsCare.Api.DTOs.CareGroups;

namespace SeasonsCare.Api.Validations.CareGroups
{
    public class UpdateCareGroupRequestValidator : AbstractValidator<UpdateCareGroupRequest>
    {
        public UpdateCareGroupRequestValidator()
        {
            RuleFor(x => x.Name)
                .NotEmpty().WithMessage("Name is required.")
                .MaximumLength(100).WithMessage("Name must not exceed 100 characters.");

            RuleFor(x => x.RecipientName)
                .NotEmpty().WithMessage("RecipientName is required.")
                .MaximumLength(100).WithMessage("RecipientName must not exceed 100 characters.");

            RuleFor(x => x.Description)
                .MaximumLength(500).WithMessage("Description must not exceed 500 characters.");

            RuleFor(x => x.HealthStatus)
                .MaximumLength(1000).WithMessage("HealthStatus must not exceed 1000 characters.");
        }
    }
}
