using System;
using FluentValidation;
using SeasonsCare.Api.DTOs.CareGroups;

namespace SeasonsCare.Api.Validations.CareGroups
{
    public class UpdateCareGroupRequestValidator : AbstractValidator<UpdateCareGroupRequest>
    {
        public UpdateCareGroupRequestValidator()
        {
            RuleFor(x => x.Name)
                .NotEmpty().WithMessage("群組名稱為必填")
                .MaximumLength(100).WithMessage("群組名稱長度不可超過 100 字元");

            RuleFor(x => x.RecipientName)
                .NotEmpty().WithMessage("被照護者名稱為必填")
                .MaximumLength(100).WithMessage("被照護者名稱長度不可超過 100 字元");

            RuleFor(x => x.RecipientGender)
                .NotEmpty().WithMessage("被照護者性別為必填")
                .MaximumLength(20).WithMessage("被照護者性別長度不可超過 20 字元");

            RuleFor(x => x.RecipientBirthDate)
                .NotNull()
                .WithMessage("出生年月日為必填")
                .Must(x => x <= DateOnly.FromDateTime(DateTime.Today))
                .WithMessage("出生年月日不可晚於今天");

            RuleFor(x => x.Description)
                .MaximumLength(500).WithMessage("描述長度不可超過 500 字元");

            RuleFor(x => x.HealthStatus)
                .MaximumLength(1000).WithMessage("健康狀況長度不可超過 1000 字元");
        }
    }
}
