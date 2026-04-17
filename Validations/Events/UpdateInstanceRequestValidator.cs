using FluentValidation;
using SeasonsCare.Api.DTOs.Events;

namespace SeasonsCare.Api.Validations.Events
{
    public class UpdateInstanceRequestValidator : AbstractValidator<UpdateInstanceRequest>
    {
        private static readonly string[] AllowedStatuses = { "completed", "cancelled", "pending" };

        public UpdateInstanceRequestValidator()
        {
            RuleFor(x => x.ScheduledAt)
                .NotEqual(default(System.DateTime)).WithMessage("scheduledAt 為必填");

            RuleFor(x => x.Status)
                .Must(s => AllowedStatuses.Contains(s!.Trim().ToLowerInvariant()))
                .When(x => !string.IsNullOrWhiteSpace(x.Status))
                .WithMessage("status 必須為 completed / cancelled / pending");

            RuleForEach(x => x.Participants)
                .NotEmpty().WithMessage("participants 不可包含空白值");

            RuleFor(x => x)
                .Must(x => !string.IsNullOrWhiteSpace(x.Status) || x.Description != null || x.Participants != null)
                .WithMessage("至少須提供 status、description 或 participants 其中一項");
        }
    }
}
