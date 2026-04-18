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
    /// FME-1 ~ FME-6 的分派層；只做 DTO 轉換與底層 Service 呼叫。
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
                Description = request.Description,
                StartsAt = request.StartsAt.UtcDateTime,
                DurationMinutes = null,
                RepeatPattern = request.RepeatPattern,
                RepeatInterval = 1,
                DaysOfWeek = request.DaysOfWeek,
                EndType = EventSeriesEndType.Never,
                EndAt = null,
                OccurrenceCount = null,
                Participants = request.Participants,
                IsImportant = request.IsImportant
            };
            var created = await _seriesService.CreateSeriesAsync(currentUserId, careGroupId, seriesRequest);
            return MapToEventResponse(created);
        }

        // FME-2：取得（展開後）
        public async Task<IReadOnlyList<EventOccurrenceItem>> GetEventsAsync(Guid currentUserId, Guid careGroupId, GetEventsRequest request)
        {
            var occurrences = await _occurrenceService.GetOccurrencesAsync(currentUserId, careGroupId, request.From.UtcDateTime, request.To.UtcDateTime);
            var seriesList = await _seriesService.GetAllSeriesAsync(currentUserId, careGroupId);
            var repeatPatternBySeries = seriesList.ToDictionary(s => s.Id, s => s.RepeatPattern);

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
                Description = request.Description,
                StartsAt = request.StartsAt.UtcDateTime,
                DurationMinutes = null,
                RepeatPattern = request.RepeatPattern,
                RepeatInterval = 1,
                DaysOfWeek = request.DaysOfWeek,
                EndType = EventSeriesEndType.Never,
                EndAt = null,
                OccurrenceCount = null,
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

        // FME-5：編輯單次實例（狀態 / 描述 / 參與者）
        public async Task UpdateInstanceAsync(Guid currentUserId, Guid careGroupId, Guid eventId, UpdateInstanceRequest request)
        {
            var status = (request.Status ?? string.Empty).Trim().ToLowerInvariant();
            var hasContentEdit = request.Description != null || request.Participants != null;

            var scheduledAtUtc = request.ScheduledAt.UtcDateTime;

            // 純狀態切換（沒有 description / participants）
            if (!hasContentEdit)
            {
                switch (status)
                {
                    case "completed":
                        await _occurrenceService.CompleteOccurrenceAsync(currentUserId, careGroupId, eventId, scheduledAtUtc);
                        return;
                    case "cancelled":
                        await _occurrenceService.CancelOccurrenceAsync(currentUserId, careGroupId, eventId, scheduledAtUtc);
                        return;
                    case "pending":
                        await _occurrenceService.ClearOccurrenceOverrideAsync(currentUserId, careGroupId, eventId, scheduledAtUtc);
                        return;
                    case "":
                        throw new DomainException("至少須提供 status、description 或 participants 其中一項", "VALIDATION_FAILED", 400);
                    default:
                        throw new DomainException("status 必須為 completed / cancelled / pending", "VALIDATION_FAILED", 400);
                }
            }

            // 含描述 / 參與者變更：走單次 override 更新
            EventOccurrenceStatus? occurrenceStatus = status switch
            {
                "" => null,
                "completed" => EventOccurrenceStatus.Completed,
                "cancelled" => EventOccurrenceStatus.Cancelled,
                "pending" => EventOccurrenceStatus.Scheduled,
                _ => throw new DomainException("status 必須為 completed / cancelled / pending", "VALIDATION_FAILED", 400)
            };

            await _occurrenceService.UpdateOccurrenceAsync(currentUserId, careGroupId, new UpdateEventOccurrenceRequest
            {
                EventSeriesId = eventId,
                ScheduledStartAt = scheduledAtUtc,
                Description = request.Description,
                Participants = request.Participants,
                Status = occurrenceStatus
            });
        }

        // FME-6：取得單次狀態
        public async Task<EventOccurrenceItem> GetInstanceStatusAsync(Guid currentUserId, Guid careGroupId, Guid eventId, DateTimeOffset scheduledAt)
        {
            var scheduledAtUtc = scheduledAt.UtcDateTime;
            var occurrences = await _occurrenceService.GetOccurrencesAsync(currentUserId, careGroupId, scheduledAtUtc, scheduledAtUtc);
            var target = occurrences.FirstOrDefault(o => o.EventSeriesId == eventId && o.ScheduledStartAt.UtcDateTime == scheduledAtUtc);
            if (target == null)
            {
                throw new DomainException("找不到指定的事件實例", "NOT_FOUND", 404);
            }

            var seriesList = await _seriesService.GetAllSeriesAsync(currentUserId, careGroupId);
            var repeatPatternBySeries = seriesList.ToDictionary(s => s.Id, s => s.RepeatPattern);
            return MapToOccurrenceItem(target, repeatPatternBySeries);
        }

        private static EventResponse MapToEventResponse(EventSeriesResponse series)
        {
            return new EventResponse
            {
                Id = series.Id,
                Title = series.Title,
                Description = series.Description,
                StartsAt = series.StartsAt,
                RepeatPattern = series.RepeatPattern,
                DaysOfWeek = series.DaysOfWeek,
                Participants = series.Participants ?? new List<string>(),
                IsImportant = series.IsImportant,
                CareGroupId = series.CareGroupId,
                CreatedAt = series.CreatedAt,
                UpdatedAt = series.UpdatedAt,
                CreatedBy = series.CreatedBy
            };
        }

        private static EventOccurrenceItem MapToOccurrenceItem(EventOccurrenceResponse occurrence, IReadOnlyDictionary<Guid, EventRepeatPattern> repeatPatternBySeries)
        {
            repeatPatternBySeries.TryGetValue(occurrence.EventSeriesId, out var pattern);
            return new EventOccurrenceItem
            {
                EventSeriesId = occurrence.EventSeriesId,
                Title = occurrence.Title,
                Description = occurrence.Description,
                ScheduledAt = occurrence.ScheduledStartAt,
                Participants = occurrence.Participants ?? new List<string>(),
                Status = FormatOccurrenceStatus(occurrence.Status),
                IsImportant = occurrence.IsImportant,
                RepeatPattern = pattern
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
