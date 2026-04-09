using FluentValidation;
using SeasonsCare.Api.DTOs.AiHealthInsights;

namespace SeasonsCare.Api.Validations.AiHealthInsights
{
    public class SaveAiHealthInsightRequestValidator : AbstractValidator<SaveAiHealthInsightRequest>
    {
        public SaveAiHealthInsightRequestValidator()
        {
            RuleFor(x => x.ReportType)
                .NotEmpty().WithMessage("reportType 為必填")
                .MaximumLength(50).WithMessage("reportType 長度不可超過 50");

            RuleFor(x => x.DateFrom)
                .NotEmpty().WithMessage("dateFrom 為必填");

            RuleFor(x => x.DateTo)
                .NotEmpty().WithMessage("dateTo 為必填");

            RuleFor(x => x)
                .Must(x => x.DateFrom <= x.DateTo)
                .WithMessage("dateFrom 不可晚於 dateTo");

            RuleFor(x => x.OverallSummary)
                .NotEmpty().WithMessage("overallSummary 為必填");

            RuleFor(x => x.KeyInsights)
                .NotEmpty().WithMessage("keyInsights 為必填");

            RuleFor(x => x.Recommendations)
                .NotEmpty().WithMessage("recommendations 為必填");

            RuleFor(x => x.SourceDataHash)
                .MaximumLength(128).WithMessage("sourceDataHash 長度不可超過 128")
                .When(x => !string.IsNullOrWhiteSpace(x.SourceDataHash));

            RuleFor(x => x.ModelName)
                .MaximumLength(100).WithMessage("modelName 長度不可超過 100")
                .When(x => !string.IsNullOrWhiteSpace(x.ModelName));

            RuleFor(x => x.PromptVersion)
                .MaximumLength(50).WithMessage("promptVersion 長度不可超過 50")
                .When(x => !string.IsNullOrWhiteSpace(x.PromptVersion));
        }
    }
}
