using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using SeasonsCare.Api.DTOs.Common;
using SeasonsCare.Api.DTOs.EventOccurrences;
using SeasonsCare.Api.DTOs.Events;
using SeasonsCare.Api.DTOs.EventSeries;
using SeasonsCare.Api.Exceptions;
using SeasonsCare.Api.Models.Enums;

namespace SeasonsCare.Api.Services
{
    /// <summary>
    /// Facade — 將 6 支對前端的 FME API 分派到 IEventSeriesService / IEventOccurrenceService。
    /// 不含領域邏輯；僅做 DTO 轉換 + 分派。
    /// </summary>
    public class EventFacadeService : IEventFacadeService
    {
        private readonly IEventSeriesService _seriesService;
        private readonly IEventOccurrenceService _occurrenceService;

        public EventFacadeService(IEventSeriesService seriesService, IEventOccurrenceService occurrenceService)
        {
            _seriesService = seriesService;
            _occurrenceService = occurrenceService;
        }

        // FME-1：新增
        public async Task<EventResponse> CreateEventAsync(Guid currentUserId, Guid careGroupId, CreateEventRequest request)
        {
            var seriesRequest = new CreateEventSeriesRequest
            {
                Title = request.Title,
                Description = request.Notes,
                StartsAt = request.ScheduledAt,
                RepeatPattern = ParseRepeatPattern(request.RepeatPattern),
                RepeatInterval = 1,
                DaysOfWeek = BuildDefaultDaysOfWeek(request.RepeatPattern, request.ScheduledAt),
                EndType = EventSeriesEndType.Never,
                Participants = request.Participants,
                IsImportant = request.IsImportant
            };
            var created = await _seriesService.CreateSeriesAsync(currentUserId, careGroupId, seriesRequest);
            return MapToEventResponse(created);
        }

        // FME-2：取得（展開後）
        public async Task<IReadOnlyList<EventOccurrenceItem>> GetEventsAsync(Guid currentUserId, Guid careGroupId, GetEventsRequest request)
        {
            var occurrences = await _occurrenceService.GetOccurrencesAsync(currentUserId, careGroupId, request.From, request.To);
            var seriesList = await _seriesService.GetAllSeriesAsync(currentUserId, careGroupId);
            var repeatPatternBySeries = seriesList.ToDictionary(s => s.Id, s => FormatRepeatPattern(s.RepeatPattern));

            return occurrences
                .Select(o => MapToOccurrenceItem(o, repeatPatternBySeries))
                .ToList();
        }

        // FME-3：編輯整個系列
        public async Task<EventResponse> UpdateEventAsync(Guid currentUserId, Guid careGroupId, Guid eventId, UpdateEventRequest request)
        {
            var seriesRequest = new UpdateEventSeriesRequest
            {
                Title = request.Title,
                Description = request.Notes,
                StartsAt = request.ScheduledAt,
                RepeatPattern = ParseRepeatPattern(request.RepeatPattern),
                RepeatInterval = 1,
                DaysOfWeek = BuildDefaultDaysOfWeek(request.RepeatPattern, request.ScheduledAt),
                EndType = EventSeriesEndType.Never,
                Participants = request.Participants,
                IsImportant = request.IsImportant
            };
            var updated = await _seriesService.UpdateSeriesAsync(currentUserId, careGroupId, eventId, seriesRequest);
            return MapToEventResponse(updated);
        }

        // FME-4：刪除整個系列
        public Task DeleteEventAsync(Guid currentUserId, Guid careGroupId, Guid eventId)
        {
            return _seriesService.DeleteSeriesAsync(currentUserId, careGroupId, eventId);
        }

        // FME-5：編輯單次狀態
        public async Task UpdateInstanceStatusAsync(Guid currentUserId, Guid careGroupId, Guid eventId, UpdateInstanceStatusRequest request)
        {
            var status = (request.Status ?? string.Empty).Trim().ToLowerInvariant();
            switch (status)
            {
                case "completed":
                    await _occurrenceService.CompleteOccurrenceAsync(currentUserId, careGroupId, eventId, request.ScheduledAt);
                    break;
                case "cancelled":
                    await _occurrenceService.CancelOccurrenceAsync(currentUserId, careGroupId, eventId, request.ScheduledAt);
                    break;
                case "pending":
                    await _occurrenceService.ClearOccurrenceOverrideAsync(currentUserId, careGroupId, eventId, request.ScheduledAt);
                    break;
                default:
                    throw new DomainException("status 必須為 completed / cancelled / pending", "VALIDATION_FAILED", 400);
            }
        }

        // FME-6：取得單次狀態
        public async Task<EventOccurrenceItem> GetInstanceStatusAsync(Guid currentUserId, Guid careGroupId, Guid eventId, DateTime scheduledAt)
        {
            var occurrences = await _occurrenceService.GetOccurrencesAsync(currentUserId, careGroupId, scheduledAt, scheduledAt);
            var target = occurrences.FirstOrDefault(o => o.EventSeriesId == eventId && o.ScheduledStartAt.UtcDateTime == scheduledAt.ToUniversalTime());
            if (target == null)
            {
                throw new DomainException("找不到指定的事件實例", "NOT_FOUND", 404);
            }

            var seriesList = await _seriesService.GetAllSeriesAsync(currentUserId, careGroupId);
            var repeatPatternBySeries = seriesList.ToDictionary(s => s.Id, s => FormatRepeatPattern(s.RepeatPattern));
            return MapToOccurrenceItem(target, repeatPatternBySeries);
        }

        private static EventRepeatPattern ParseRepeatPattern(string? value)
        {
            return (value ?? "none").Trim().ToLowerInvariant() switch
            {
                "none" => EventRepeatPattern.None,
                "daily" => EventRepeatPattern.Daily,
                "weekly" => EventRepeatPattern.Weekly,
                "weeklyday" => EventRepeatPattern.Weekly,
                "monthly" => EventRepeatPattern.Monthly,
                _ => throw new DomainException("repeatPattern 必須為 none / daily / weekly / monthly", "VALIDATION_FAILED", 400)
            };
        }

        private static string FormatRepeatPattern(EventRepeatPattern pattern)
        {
            return pattern switch
            {
                EventRepeatPattern.None => "none",
                EventRepeatPattern.Daily => "daily",
                EventRepeatPattern.Weekly => "weekly",
                EventRepeatPattern.Monthly => "monthly",
                _ => "none"
            };
        }

        private static List<DayOfWeek>? BuildDefaultDaysOfWeek(string? repeatPattern, DateTime scheduledAt)
        {
            // 設計稿不含「選星期」欄位。Weekly 時自動取 scheduledAt 當天的星期。
            if (ParseRepeatPattern(repeatPattern) != EventRepeatPattern.Weekly)
            {
                return null;
            }

            var localDay = scheduledAt.Kind == DateTimeKind.Utc
                ? scheduledAt.ToLocalTime().DayOfWeek
                : scheduledAt.DayOfWeek;
            return new List<DayOfWeek> { localDay };
        }

        private static EventResponse MapToEventResponse(EventSeriesResponse series)
        {
            return new EventResponse
            {
                Id = series.Id,
                Title = series.Title,
                ScheduledAt = series.StartsAt,
                RepeatPattern = FormatRepeatPattern(series.RepeatPattern),
                Participants = series.Participants ?? new List<string>(),
                IsImportant = series.IsImportant,
                Notes = series.Description,
                CareGroupId = series.CareGroupId,
                CreatedAt = series.CreatedAt,
                UpdatedAt = series.UpdatedAt,
                CreatedBy = series.CreatedBy
            };
        }

        private static EventOccurrenceItem MapToOccurrenceItem(EventOccurrenceResponse occurrence, IReadOnlyDictionary<Guid, string> repeatPatternBySeries)
        {
            repeatPatternBySeries.TryGetValue(occurrence.EventSeriesId, out var pattern);
            return new EventOccurrenceItem
            {
                Id = occurrence.Id,
                EventSeriesId = occurrence.EventSeriesId,
                Title = occurrence.Title,
                ScheduledAt = occurrence.ScheduledStartAt,
                Participants = occurrence.Participants ?? new List<string>(),
                Status = FormatOccurrenceStatus(occurrence.Status),
                IsImportant = occurrence.IsImportant,
                Notes = occurrence.Description,
                RepeatPattern = pattern ?? "none",
                HasOverrides = occurrence.HasOverrides
            };
        }

        private static string FormatOccurrenceStatus(EventOccurrenceStatus status)
        {
            return status switch
            {
                EventOccurrenceStatus.Scheduled => "pending",
                EventOccurrenceStatus.Completed => "completed",
                EventOccurrenceStatus.Cancelled => "cancelled",
                EventOccurrenceStatus.Skipped => "skipped",
                _ => "pending"
            };
        }
    }
}
