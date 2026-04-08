using FluentValidation;
using SeasonsCare.Api.DTOs.EventOccurrences;

namespace SeasonsCare.Api.Validations.EventOccurrences
{
    public class GetEventOccurrencesRequestValidator : AbstractValidator<GetEventOccurrencesRequest>
    {
        public GetEventOccurrencesRequestValidator()
        {
            RuleFor(x => x.From)
                .NotEmpty().WithMessage("from 為必填");

            RuleFor(x => x.To)
                .NotEmpty().WithMessage("to 為必填");

            RuleFor(x => x)
                .Must(x => x.To >= x.From)
                .WithMessage("to 必須大於或等於 from");
        }
    }
}
