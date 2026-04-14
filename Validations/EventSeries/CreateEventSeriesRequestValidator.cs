using System;
using FluentValidation;
using SeasonsCare.Api.Config;
using SeasonsCare.Api.DTOs.EventSeries;
using SeasonsCare.Api.Models.Enums;

namespace SeasonsCare.Api.Validations.EventSeries
{
    public class CreateEventSeriesRequestValidator : AbstractValidator<CreateEventSeriesRequest>
    {
        public CreateEventSeriesRequestValidator()
        {
            RuleFor(x => x.Title)
                .NotEmpty().WithMessage("標題為必填")
                .MaximumLength(100).WithMessage("標題長度不可超過 100 字元");

            RuleFor(x => x.DurationMinutes)
                .GreaterThan(0).When(x => x.DurationMinutes.HasValue)
                .WithMessage("durationMinutes 必須大於 0");

            RuleFor(x => x.RepeatInterval)
                .GreaterThan(0).WithMessage("repeatInterval 必須大於 0");

            RuleForEach(x => x.Participants)
                .NotEmpty().WithMessage("participants 不可包含空白值");

            RuleFor(x => x.EndAt)
                .NotNull()
                .When(x => x.EndType == EventSeriesEndType.OnDate)
                .WithMessage("當 endType 為 OnDate 時，endAt 為必填");

            RuleFor(x => x.OccurrenceCount)
                .NotNull()
                .GreaterThan(0)
                .When(x => x.EndType == EventSeriesEndType.AfterOccurrences)
                .WithMessage("當 endType 為 AfterOccurrences 時，occurrenceCount 必須大於 0");

            RuleFor(x => x.DaysOfWeek)
                .Must(x => x != null && x.Count > 0)
                .When(x => x.RepeatPattern == EventRepeatPattern.Weekly)
                .WithMessage("當 repeatPattern 為 Weekly 時，daysOfWeek 至少要有一個星期值");

            RuleFor(x => x.StartsAt)
                .LessThanOrEqualTo(_ => TimeHelper.TaiwanNow.AddYears(5))
                .WithMessage("startsAt 不可超出合理範圍");
        }
    }
}
