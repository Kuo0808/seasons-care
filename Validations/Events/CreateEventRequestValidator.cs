using System;
using FluentValidation;
using SeasonsCare.Api.DTOs.Events;
using SeasonsCare.Api.Models.Enums;

namespace SeasonsCare.Api.Validations.Events
{
    public class CreateEventRequestValidator : AbstractValidator<CreateEventRequest>
    {
        public CreateEventRequestValidator()
        {
            RuleFor(x => x.Title)
                .NotEmpty().WithMessage("title 為必填")
                .MaximumLength(100).WithMessage("title 長度不可超過 100 字元");

            RuleForEach(x => x.Participants)
                .NotEmpty().WithMessage("participants 不可包含空白值");

            RuleFor(x => x.DaysOfWeek)
                .Must(x => x != null && x.Count > 0)
                .When(x => x.RepeatPattern == EventRepeatPattern.Weekly)
                .WithMessage("當 repeatPattern 為 weeklyDay 時，daysOfWeek 至少要有一個星期值");

            RuleFor(x => x.StartsAt)
                .LessThanOrEqualTo(_ => DateTimeOffset.UtcNow.AddYears(5))
                .WithMessage("startsAt 不可超出合理範圍");
        }
    }
}
