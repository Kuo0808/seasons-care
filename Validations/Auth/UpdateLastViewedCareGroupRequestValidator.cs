using FluentValidation;
using SeasonsCare.Api.DTOs.Auth;

namespace SeasonsCare.Api.Validations.Auth
{
    public class UpdateLastViewedCareGroupRequestValidator : AbstractValidator<UpdateLastViewedCareGroupRequest>
    {
        public UpdateLastViewedCareGroupRequestValidator()
        {
            RuleFor(x => x.CareGroupId)
                .NotEmpty().WithMessage("照護群組 ID 為必填。");
        }
    }
}
