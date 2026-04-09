using FluentValidation;
using SeasonsCare.Api.DTOs.EventOccurrences;

namespace SeasonsCare.Api.Validations.EventOccurrences
{
    public class CompleteEventOccurrenceRequestValidator : AbstractValidator<CompleteEventOccurrenceRequest>
    {
        public CompleteEventOccurrenceRequestValidator()
        {
            RuleFor(x => x.EventSeriesId)
                .NotEmpty().WithMessage("eventSeriesId 為必填");

            RuleFor(x => x.ScheduledStartAt)
                .NotEmpty().WithMessage("scheduledStartAt 為必填");
        }
    }
}
