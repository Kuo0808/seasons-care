using FluentValidation;
using SeasonsCare.Api.DTOs.EventOccurrences;

namespace SeasonsCare.Api.Validations.EventOccurrences
{
    public class UpdateEventOccurrenceRequestValidator : AbstractValidator<UpdateEventOccurrenceRequest>
    {
        public UpdateEventOccurrenceRequestValidator()
        {
            RuleFor(x => x.EventSeriesId)
                .NotEmpty().WithMessage("eventSeriesId 為必填");

            RuleFor(x => x.ScheduledStartAt)
                .NotEmpty().WithMessage("scheduledStartAt 為必填");

            RuleFor(x => x)
                .Must(HasAtLeastOneEditableField)
                .WithMessage("至少需要提供一個可更新欄位");

            RuleFor(x => x)
                .Must(x => !x.EndsAt.HasValue || !x.StartsAt.HasValue || x.EndsAt.Value >= x.StartsAt.Value)
                .WithMessage("endsAt 不可早於 startsAt");
        }

        private static bool HasAtLeastOneEditableField(UpdateEventOccurrenceRequest request)
        {
            return request.Title != null ||
                   request.Description != null ||
                   request.StartsAt.HasValue ||
                   request.EndsAt.HasValue ||
                   request.Participants != null ||
                   request.Status.HasValue ||
                   request.IsImportant.HasValue;
        }
    }
}
