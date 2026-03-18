using FluentValidation;
using SeasonsCare.Api.DTOs.CareGroups;

namespace SeasonsCare.Api.Validations.CareGroups
{
    public class CreateCareGroupRequestValidator : AbstractValidator<CreateCareGroupRequest>
    {
        public CreateCareGroupRequestValidator()
        {
            RuleFor(x => x.Name)
                .NotEmpty().WithMessage("群組名稱為必填項目。")
                .MaximumLength(100).WithMessage("群組名稱長度不可超過 100 個字元。");

            RuleFor(x => x.RecipientName)
                .NotEmpty().WithMessage("被照護者名稱為必填項目。")
                .MaximumLength(100).WithMessage("被照護者名稱長度不可超過 100 個字元。");

            RuleFor(x => x.Description)
                .MaximumLength(500).WithMessage("描述長度不可超過 500 個字元。");

            RuleFor(x => x.HealthStatus)
                .MaximumLength(1000).WithMessage("健康狀況長度不可超過 1000 個字元。");
        }
    }
}
