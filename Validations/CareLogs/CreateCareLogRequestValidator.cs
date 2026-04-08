using System;
using FluentValidation;
using SeasonsCare.Api.DTOs.CareLogs;

namespace SeasonsCare.Api.Validations.CareLogs
{
    public class CreateCareLogRequestValidator : AbstractValidator<CreateCareLogRequest>
    {
        public CreateCareLogRequestValidator()
        {
            RuleFor(x => x.Title)
                .NotEmpty().WithMessage("標題為必填")
                .MaximumLength(100).WithMessage("標題長度不可超過 100 字元");

            RuleFor(x => x.RepeatPattern)
                .MaximumLength(50).WithMessage("重複規則長度不可超過 50 字元");

            RuleFor(x => x.Status)
                .MaximumLength(50).WithMessage("狀態長度不可超過 50 字元");

            RuleFor(x => x.StartsAt)
                .LessThanOrEqualTo(_ => DateTime.UtcNow.AddMinutes(5))
                .When(x => x.StartsAt.HasValue)
                .WithMessage("開始時間不可晚於目前時間太多");

            RuleForEach(x => x.Participants)
                .NotEmpty().WithMessage("participants 不可包含空白值");
        }
    }
}
