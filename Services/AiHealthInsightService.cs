using System;
using System.Threading.Tasks;
using SeasonsCare.Api.Config;
using SeasonsCare.Api.DTOs.AiHealthInsights;
using SeasonsCare.Api.Exceptions;
using SeasonsCare.Api.Models.Entities;
using SeasonsCare.Api.Repositories;

namespace SeasonsCare.Api.Services
{
    public class AiHealthInsightService : IAiHealthInsightService
    {
        private readonly IAiHealthInsightRepository _aiHealthInsightRepository;
        private readonly ICareGroupRepository _careGroupRepository;

        public AiHealthInsightService(IAiHealthInsightRepository aiHealthInsightRepository, ICareGroupRepository careGroupRepository)
        {
            _aiHealthInsightRepository = aiHealthInsightRepository;
            _careGroupRepository = careGroupRepository;
        }

        public async Task<AiHealthInsightResponse> SaveInsightAsync(Guid currentUserId, Guid careGroupId, SaveAiHealthInsightRequest request)
        {
            await CheckMembershipAsync(careGroupId, currentUserId);

            var normalizedDateFrom = NormalizeTimestamp(request.DateFrom);
            var normalizedDateTo = NormalizeTimestamp(request.DateTo);
            var now = NormalizeTimestamp(TimeHelper.UtcNow);
            var reportType = request.ReportType.Trim();

            // 同一照護群組、報告類型與分析區間只保留一份最新快照。
            var existing = await _aiHealthInsightRepository.GetByUniqueKeyAsync(careGroupId, reportType, normalizedDateFrom, normalizedDateTo);
            if (existing == null)
            {
                var insight = new AiHealthInsight
                {
                    CareGroupId = careGroupId,
                    ReportType = reportType,
                    DateFrom = normalizedDateFrom,
                    DateTo = normalizedDateTo,
                    OverallSummary = request.OverallSummary,
                    TodaySummary = request.TodaySummary,
                    KeyInsights = request.KeyInsights,
                    Recommendations = request.Recommendations,
                    TrendLabels = request.TrendLabels,
                    SourceDataHash = request.SourceDataHash,
                    ModelName = request.ModelName,
                    PromptVersion = request.PromptVersion,
                    GeneratedAt = now,
                    CreatedAt = now,
                    UpdatedAt = now,
                    CreatedBy = currentUserId.ToString()
                };

                await _aiHealthInsightRepository.AddAsync(insight);
                await _aiHealthInsightRepository.SaveChangesAsync();
                return MapToResponse(insight);
            }

            existing.OverallSummary = request.OverallSummary;
            existing.TodaySummary = request.TodaySummary;
            existing.KeyInsights = request.KeyInsights;
            existing.Recommendations = request.Recommendations;
            existing.TrendLabels = request.TrendLabels;
            existing.SourceDataHash = request.SourceDataHash;
            existing.ModelName = request.ModelName;
            existing.PromptVersion = request.PromptVersion;
            existing.GeneratedAt = now;
            existing.UpdatedAt = now;

            await _aiHealthInsightRepository.UpdateAsync(existing);
            await _aiHealthInsightRepository.SaveChangesAsync();
            return MapToResponse(existing);
        }

        public async Task<AiHealthInsightResponse> GetLatestInsightAsync(Guid currentUserId, Guid careGroupId, string? reportType)
        {
            await CheckMembershipAsync(careGroupId, currentUserId);

            var insight = await _aiHealthInsightRepository.GetLatestAsync(careGroupId, reportType?.Trim());
            if (insight == null)
            {
                throw new DomainException("AI health insight not found.", "NOT_FOUND", 404);
            }

            return MapToResponse(insight);
        }

        private async Task CheckMembershipAsync(Guid careGroupId, Guid userId)
        {
            var isMember = await _careGroupRepository.IsMemberAsync(careGroupId, userId);
            if (!isMember)
            {
                throw new DomainException("You are not a member of this care group.", "FORBIDDEN", 403);
            }
        }

        private static AiHealthInsightResponse MapToResponse(AiHealthInsight insight)
        {
            return new AiHealthInsightResponse
            {
                Id = insight.Id,
                CareGroupId = insight.CareGroupId,
                ReportType = insight.ReportType,
                DateFrom = TimeHelper.ToTaiwanOffset(insight.DateFrom),
                DateTo = TimeHelper.ToTaiwanOffset(insight.DateTo),
                OverallSummary = insight.OverallSummary,
                TodaySummary = insight.TodaySummary,
                KeyInsights = insight.KeyInsights,
                Recommendations = insight.Recommendations,
                TrendLabels = insight.TrendLabels,
                SourceDataHash = insight.SourceDataHash,
                ModelName = insight.ModelName,
                PromptVersion = insight.PromptVersion,
                GeneratedAt = TimeHelper.ToTaiwanOffset(insight.GeneratedAt),
                CreatedAt = TimeHelper.ToTaiwanOffset(insight.CreatedAt),
                UpdatedAt = TimeHelper.ToTaiwanOffset(insight.UpdatedAt),
                CreatedBy = insight.CreatedBy
            };
        }

        private static DateTime NormalizeTimestamp(DateTime value)
        {
            var utcValue = value.Kind == DateTimeKind.Utc ? value : value.ToUniversalTime();
            return new DateTime(utcValue.Ticks - (utcValue.Ticks % TimeSpan.TicksPerMillisecond), DateTimeKind.Utc);
        }
    }
}
