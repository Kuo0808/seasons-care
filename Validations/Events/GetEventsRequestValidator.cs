using System;
using FluentValidation;
using SeasonsCare.Api.DTOs.Events;

namespace SeasonsCare.Api.Validations.Events
{
    public class GetEventsRequestValidator : AbstractValidator<GetEventsRequest>
    {
        public GetEventsRequestValidator()
        {
            RuleFor(x => x.From)
                .Must(x => x != default)
                .WithMessage("from 為必填，請帶入查詢區間起點。");

            RuleFor(x => x.To)
                .Must(x => x != default)
                .WithMessage("to 為必填，請帶入查詢區間終點。");

            RuleFor(x => x)
                .Must(x => x.From != default && x.To != default && x.To >= x.From)
                .WithMessage("to 必須大於或等於 from。");

            RuleFor(x => x)
                .Must(x => x.From == default || x.To == default || (x.To - x.From) <= TimeSpan.FromDays(366))
                .WithMessage("查詢區間不可超過 366 天。");
        }
    }
}
