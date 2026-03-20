using FluentValidation;
using SeasonsCare.Api.DTOs.CareLogs;

namespace SeasonsCare.Api.Validations.CareLogs
{
    public class UpdateCareLogRequestValidator : AbstractValidator<UpdateCareLogRequest>
    {
        public UpdateCareLogRequestValidator()
        {
            RuleFor(x => x.Title)
                .NotEmpty().WithMessage("請輸入照護日誌標題")
                .MaximumLength(100).WithMessage("照護日誌標題不可超過 100 字");

            RuleFor(x => x.LogType)
                .MaximumLength(50).WithMessage("日誌類型不可超過 50 字");

            RuleFor(x => x.RecordDate)
                .LessThanOrEqualTo(_ => DateTime.UtcNow.AddMinutes(5))
                .When(x => x.RecordDate.HasValue)
                .WithMessage("紀錄時間不可晚於目前時間太多");

            RuleFor(x => x.UpdatedAt)
                .NotNull().WithMessage("更新照護日誌時必須提供 UpdatedAt");
        }
    }
}
