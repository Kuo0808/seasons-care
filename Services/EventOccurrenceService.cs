using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using SeasonsCare.Api.Config;
using SeasonsCare.Api.DTOs.EventOccurrences;
using SeasonsCare.Api.Exceptions;
using SeasonsCare.Api.Models.Entities;
using SeasonsCare.Api.Models.Enums;
using SeasonsCare.Api.Repositories;

namespace SeasonsCare.Api.Services
{
    public class EventOccurrenceService : IEventOccurrenceService
    {
        private readonly IEventSeriesRepository _eventSeriesRepository;
        private readonly IEventOccurrenceRepository _eventOccurrenceRepository;
        private readonly ICareGroupRepository _careGroupRepository;
        private readonly INotificationService _notificationService;

        public EventOccurrenceService(
            IEventSeriesRepository eventSeriesRepository,
            IEventOccurrenceRepository eventOccurrenceRepository,
            ICareGroupRepository careGroupRepository)
            : this(eventSeriesRepository, eventOccurrenceRepository, careGroupRepository, NullNotificationService.Instance)
        {
        }

        public EventOccurrenceService(
            IEventSeriesRepository eventSeriesRepository,
            IEventOccurrenceRepository eventOccurrenceRepository,
            ICareGroupRepository careGroupRepository,
            INotificationService notificationService)
        {
            _eventSeriesRepository = eventSeriesRepository;
            _eventOccurrenceRepository = eventOccurrenceRepository;
            _careGroupRepository = careGroupRepository;
            _notificationService = notificationService;
        }

        public async Task<IReadOnlyList<EventOccurrenceResponse>> GetOccurrencesAsync(Guid currentUserId, Guid careGroupId, DateTime from, DateTime to)
        {
            if (to < from)
            {
                throw new DomainException("The to value must be greater than or equal to from.", "VALIDATION_FAILED", 400);
            }

            await CheckMembershipAsync(careGroupId, currentUserId);

            var normalizedFrom = NormalizeTimestamp(from);
            var normalizedTo = NormalizeTimestamp(to);
            var seriesList = await _eventSeriesRepository.GetAllByCareGroupIdAsync(careGroupId);
            var overrides = await _eventOccurrenceRepository.GetByRangeAsync(careGroupId, normalizedFrom, normalizedTo);
            var overrideLookup = overrides.ToDictionary(x => (x.EventSeriesId, NormalizeTimestamp(x.OccurrenceKeyStartAt)));

            var items = new List<EventOccurrenceResponse>();
            foreach (var series in seriesList)
            {
                items.AddRange(ExpandSeriesOccurrences(series, normalizedFrom, normalizedTo, overrideLookup));
            }

            return items
                .OrderBy(x => x.ScheduledStartAt)
                .ThenBy(x => x.Title, StringComparer.OrdinalIgnoreCase)
                .ToList();
        }

        public Task CancelOccurrenceAsync(Guid currentUserId, Guid careGroupId, Guid eventSeriesId, DateTime scheduledStartAt)
        {
            return UpsertOccurrenceOverrideAsync(
                currentUserId,
                careGroupId,
                eventSeriesId,
                scheduledStartAt,
                overrideStatus: EventOccurrenceStatus.Cancelled);
        }

        public async Task CompleteOccurrenceAsync(Guid currentUserId, Guid careGroupId, Guid eventSeriesId, DateTime scheduledStartAt)
        {
            var result = await UpsertOccurrenceOverrideAsync(
                currentUserId,
                careGroupId,
                eventSeriesId,
                scheduledStartAt,
                overrideStatus: EventOccurrenceStatus.Completed);

            if (result.Series.IsImportant &&
                result.PreviousStatus != EventOccurrenceStatus.Completed &&
                result.Occurrence.Status == EventOccurrenceStatus.Completed)
            {
                var effectiveStartAt = NormalizeTimestamp(result.Occurrence.OverrideStartAt ?? result.OccurrenceKeyStartAt);
                var effectiveTitle = result.Occurrence.OverrideTitle ?? result.Series.Title;
                await _notificationService.NotifyImportantTaskCompletedAsync(
                    currentUserId,
                    careGroupId,
                    result.Series.Id,
                    effectiveTitle,
                    effectiveStartAt);
            }
        }

        public async Task<EventOccurrenceResponse> UpdateOccurrenceAsync(Guid currentUserId, Guid careGroupId, UpdateEventOccurrenceRequest request)
        {
            var updated = await UpsertOccurrenceOverrideAsync(
                currentUserId,
                careGroupId,
                request.EventSeriesId,
                request.ScheduledStartAt,
                request.Title,
                request.Description,
                request.StartsAt,
                request.EndsAt,
                request.Participants,
                request.Status,
                request.IsImportant);

            return BuildOccurrenceResponse(updated.Series, updated.OccurrenceKeyStartAt, new Dictionary<(Guid SeriesId, DateTime ScheduledStartAt), EventOccurrence>
            {
                [(updated.Series.Id, updated.OccurrenceKeyStartAt)] = updated.Occurrence
            });
        }

        public async Task ClearOccurrenceOverrideAsync(Guid currentUserId, Guid careGroupId, Guid eventSeriesId, DateTime scheduledStartAt)
        {
            await CheckMembershipAsync(careGroupId, currentUserId);

            var series = await GetSeriesAsync(careGroupId, eventSeriesId);
            var resolution = await ResolveOccurrenceAsync(series, scheduledStartAt);
            var existing = await _eventOccurrenceRepository.GetBySeriesIdAndOccurrenceKeyStartAtAsync(eventSeriesId, resolution.OccurrenceKeyStartAt);

            if (existing == null)
            {
                throw new DomainException("Event occurrence override not found.", "NOT_FOUND", 404);
            }

            existing.DeletedAt = NormalizeTimestamp(TimeHelper.UtcNow);
            existing.UpdatedAt = existing.DeletedAt;
            await _eventOccurrenceRepository.UpdateAsync(existing);
            await _eventOccurrenceRepository.SaveChangesAsync();
        }

        private async Task<UpsertOccurrenceResult> UpsertOccurrenceOverrideAsync(
            Guid currentUserId,
            Guid careGroupId,
            Guid eventSeriesId,
            DateTime scheduledStartAt,
            string? overrideTitle = null,
            string? overrideDescription = null,
            DateTime? overrideStartsAt = null,
            DateTime? overrideEndsAt = null,
            IReadOnlyCollection<string>? overrideParticipants = null,
            EventOccurrenceStatus? overrideStatus = null,
            bool? overrideIsImportant = null)
        {
            await CheckMembershipAsync(careGroupId, currentUserId);

            var series = await GetSeriesAsync(careGroupId, eventSeriesId);
            var resolution = await ResolveOccurrenceAsync(series, scheduledStartAt);
            var occurrenceKeyStartAt = resolution.OccurrenceKeyStartAt;
            var existing = await _eventOccurrenceRepository.GetBySeriesIdAndOccurrenceKeyStartAtAsync(eventSeriesId, occurrenceKeyStartAt);
            var now = NormalizeTimestamp(TimeHelper.UtcNow);
            var previousStatus = existing?.Status ?? EventOccurrenceStatus.Scheduled;

            if (overrideEndsAt.HasValue)
            {
                var effectiveStart = NormalizeTimestamp(overrideStartsAt ?? resolution.CurrentEffectiveStartAt);
                var effectiveEnd = NormalizeTimestamp(overrideEndsAt.Value);
                if (effectiveEnd < effectiveStart)
                {
                    throw new DomainException("The endsAt value must be greater than or equal to startsAt.", "VALIDATION_FAILED", 400);
                }
            }

            var isNewRecord = existing == null;
            if (existing == null)
            {
                existing = new EventOccurrence
                {
                    EventSeriesId = eventSeriesId,
                    CareGroupId = careGroupId,
                    OccurrenceKeyStartAt = occurrenceKeyStartAt,
                    CreatedAt = now,
                    CreatedBy = currentUserId.ToString()
                };

                await _eventOccurrenceRepository.AddAsync(existing);
            }

            existing.OverrideTitle = overrideTitle ?? existing.OverrideTitle;
            existing.OverrideDescription = overrideDescription ?? existing.OverrideDescription;
            existing.OverrideParticipants = overrideParticipants != null
                ? overrideParticipants.ToArray()
                : existing.OverrideParticipants;
            existing.OverrideStartAt = ResolveOverrideStartAt(overrideStartsAt, occurrenceKeyStartAt, existing.OverrideStartAt);
            existing.OverrideEndAt = ResolveOverrideEndAt(
                overrideEndsAt,
                existing.OverrideStartAt ?? resolution.CurrentEffectiveStartAt,
                occurrenceKeyStartAt,
                series.DurationMinutes,
                existing.OverrideEndAt);
            existing.Status = overrideStatus ?? existing.Status;
            existing.OverrideIsImportant = overrideIsImportant ?? existing.OverrideIsImportant;
            existing.UpdatedAt = now;

            if (!isNewRecord)
            {
                await _eventOccurrenceRepository.UpdateAsync(existing);
            }
            await _eventOccurrenceRepository.SaveChangesAsync();

            return new UpsertOccurrenceResult(series, existing, occurrenceKeyStartAt, previousStatus);
        }

        private async Task CheckMembershipAsync(Guid careGroupId, Guid userId)
        {
            var isMember = await _careGroupRepository.IsMemberAsync(careGroupId, userId);
            if (!isMember)
            {
                throw new DomainException("You are not a member of this care group.", "FORBIDDEN", 403);
            }
        }

        private async Task<EventSeries> GetSeriesAsync(Guid careGroupId, Guid eventSeriesId)
        {
            var series = await _eventSeriesRepository.GetByIdAsync(eventSeriesId);
            if (series == null || series.CareGroupId != careGroupId)
            {
                throw new DomainException("Event series not found.", "NOT_FOUND", 404);
            }

            return series;
        }

        private async Task<ResolvedOccurrence> ResolveOccurrenceAsync(EventSeries series, DateTime requestedScheduledStartAt)
        {
            var normalizedRequestedStartAt = NormalizeTimestamp(requestedScheduledStartAt);
            var existing = await _eventOccurrenceRepository.GetBySeriesIdAndEffectiveStartAtAsync(series.Id, normalizedRequestedStartAt);
            if (existing != null)
            {
                return new ResolvedOccurrence(
                    NormalizeTimestamp(existing.OccurrenceKeyStartAt),
                    NormalizeTimestamp(existing.OverrideStartAt ?? existing.OccurrenceKeyStartAt));
            }

            var occurrenceExists = ExpandSeriesOccurrences(
                    series,
                    normalizedRequestedStartAt,
                    normalizedRequestedStartAt,
                    new Dictionary<(Guid SeriesId, DateTime ScheduledStartAt), EventOccurrence>())
                .Any();

            if (!occurrenceExists)
            {
                throw new DomainException("Event occurrence not found in this series.", "NOT_FOUND", 404);
            }

            return new ResolvedOccurrence(normalizedRequestedStartAt, normalizedRequestedStartAt);
        }

        private static IEnumerable<EventOccurrenceResponse> ExpandSeriesOccurrences(
            EventSeries series,
            DateTime from,
            DateTime to,
            IReadOnlyDictionary<(Guid SeriesId, DateTime ScheduledStartAt), EventOccurrence> overrideLookup)
        {
            return series.RepeatPattern switch
            {
                EventRepeatPattern.None => ExpandNonRecurring(series, from, to, overrideLookup),
                EventRepeatPattern.Daily => ExpandDaily(series, from, to, overrideLookup),
                EventRepeatPattern.Weekly => ExpandWeekly(series, from, to, overrideLookup),
                EventRepeatPattern.Monthly => ExpandMonthly(series, from, to, overrideLookup),
                _ => Enumerable.Empty<EventOccurrenceResponse>()
            };
        }

        private static IEnumerable<EventOccurrenceResponse> ExpandNonRecurring(
            EventSeries series,
            DateTime from,
            DateTime to,
            IReadOnlyDictionary<(Guid SeriesId, DateTime ScheduledStartAt), EventOccurrence> overrideLookup)
        {
            var scheduledStartAt = NormalizeTimestamp(series.StartsAt);
            if (scheduledStartAt < from || scheduledStartAt > to)
            {
                yield break;
            }

            yield return BuildOccurrenceResponse(series, scheduledStartAt, overrideLookup);
        }

        private static IEnumerable<EventOccurrenceResponse> ExpandWeekly(
            EventSeries series,
            DateTime from,
            DateTime to,
            IReadOnlyDictionary<(Guid SeriesId, DateTime ScheduledStartAt), EventOccurrence> overrideLookup)
        {
            var occurrenceIndex = 0;
            foreach (var scheduledStartAt in EnumerateWeeklyOccurrences(series, to))
            {
                occurrenceIndex++;

                if (series.OccurrenceCount.HasValue && series.EndType == EventSeriesEndType.AfterOccurrences && occurrenceIndex > series.OccurrenceCount.Value)
                {
                    yield break;
                }

                var effectiveStartAt = ResolveEffectiveStartAt(series.Id, scheduledStartAt, overrideLookup);
                if (effectiveStartAt < from)
                {
                    continue;
                }

                if (effectiveStartAt > to)
                {
                    continue;
                }

                yield return BuildOccurrenceResponse(series, scheduledStartAt, overrideLookup);
            }
        }

        private static IEnumerable<EventOccurrenceResponse> ExpandDaily(
            EventSeries series,
            DateTime from,
            DateTime to,
            IReadOnlyDictionary<(Guid SeriesId, DateTime ScheduledStartAt), EventOccurrence> overrideLookup)
        {
            var occurrenceIndex = 0;
            foreach (var scheduledStartAt in EnumerateDailyOccurrences(series, to))
            {
                occurrenceIndex++;

                if (series.OccurrenceCount.HasValue &&
                    series.EndType == EventSeriesEndType.AfterOccurrences &&
                    occurrenceIndex > series.OccurrenceCount.Value)
                {
                    yield break;
                }

                var effectiveStartAt = ResolveEffectiveStartAt(series.Id, scheduledStartAt, overrideLookup);
                if (effectiveStartAt < from)
                {
                    continue;
                }

                if (effectiveStartAt > to)
                {
                    continue;
                }

                yield return BuildOccurrenceResponse(series, scheduledStartAt, overrideLookup);
            }
        }

        private static IEnumerable<EventOccurrenceResponse> ExpandMonthly(
            EventSeries series,
            DateTime from,
            DateTime to,
            IReadOnlyDictionary<(Guid SeriesId, DateTime ScheduledStartAt), EventOccurrence> overrideLookup)
        {
            var occurrenceIndex = 0;
            foreach (var scheduledStartAt in EnumerateMonthlyOccurrences(series, to))
            {
                occurrenceIndex++;

                if (series.OccurrenceCount.HasValue &&
                    series.EndType == EventSeriesEndType.AfterOccurrences &&
                    occurrenceIndex > series.OccurrenceCount.Value)
                {
                    yield break;
                }

                var effectiveStartAt = ResolveEffectiveStartAt(series.Id, scheduledStartAt, overrideLookup);
                if (effectiveStartAt < from)
                {
                    continue;
                }

                if (effectiveStartAt > to)
                {
                    continue;
                }

                yield return BuildOccurrenceResponse(series, scheduledStartAt, overrideLookup);
            }
        }

        private static IEnumerable<DateTime> EnumerateDailyOccurrences(EventSeries series, DateTime rangeEnd)
        {
            var start = NormalizeTimestamp(series.StartsAt);
            var effectiveEnd = series.EndType == EventSeriesEndType.OnDate && series.EndAt.HasValue
                ? Min(NormalizeTimestamp(series.EndAt.Value), rangeEnd)
                : rangeEnd;

            if (effectiveEnd < start)
            {
                yield break;
            }

            var current = start;
            var interval = Math.Max(series.RepeatInterval, 1);
            while (current <= effectiveEnd)
            {
                yield return current;
                current = current.AddDays(interval);
            }
        }

        private static IEnumerable<DateTime> EnumerateWeeklyOccurrences(EventSeries series, DateTime rangeEnd)
        {
            var start = NormalizeTimestamp(series.StartsAt);
            var effectiveEnd = series.EndType == EventSeriesEndType.OnDate && series.EndAt.HasValue
                ? Min(NormalizeTimestamp(series.EndAt.Value), rangeEnd)
                : rangeEnd;

            if (effectiveEnd < start)
            {
                yield break;
            }

            // DaysOfWeek is authored in Taiwan local time from the client, so we iterate the
            // calendar in Taiwan time and convert each matched date back to UTC.
            var taiwanStart = TimeHelper.ToTaiwanTime(start);
            var taiwanEnd = TimeHelper.ToTaiwanTime(effectiveEnd);

            var allowedDays = ParseDaysOfWeekMask(series.DaysOfWeekMask);
            if (allowedDays.Count == 0)
            {
                allowedDays.Add(taiwanStart.DayOfWeek);
            }

            var currentDate = taiwanStart.Date;
            while (currentDate <= taiwanEnd.Date)
            {
                var taiwanOccurrence = new DateTime(
                    currentDate.Year,
                    currentDate.Month,
                    currentDate.Day,
                    taiwanStart.Hour,
                    taiwanStart.Minute,
                    taiwanStart.Second,
                    taiwanStart.Millisecond,
                    DateTimeKind.Unspecified);

                if (allowedDays.Contains(taiwanOccurrence.DayOfWeek) &&
                    IsMatchingWeeklyInterval(taiwanStart, taiwanOccurrence, series.RepeatInterval))
                {
                    var occurrenceStart = NormalizeTimestamp(TimeHelper.TaiwanToUtc(taiwanOccurrence));
                    if (occurrenceStart >= start && occurrenceStart <= effectiveEnd)
                    {
                        yield return occurrenceStart;
                    }
                }

                currentDate = currentDate.AddDays(1);
            }
        }

        private static IEnumerable<DateTime> EnumerateMonthlyOccurrences(EventSeries series, DateTime rangeEnd)
        {
            var start = NormalizeTimestamp(series.StartsAt);
            var effectiveEnd = series.EndType == EventSeriesEndType.OnDate && series.EndAt.HasValue
                ? Min(NormalizeTimestamp(series.EndAt.Value), rangeEnd)
                : rangeEnd;

            if (effectiveEnd < start)
            {
                yield break;
            }

            var interval = Math.Max(series.RepeatInterval, 1);
            var occurrenceMonth = 0;

            while (true)
            {
                var scheduledStartAt = BuildMonthlyOccurrenceStart(start, occurrenceMonth);

                if (scheduledStartAt > effectiveEnd)
                {
                    yield break;
                }

                yield return scheduledStartAt;
                occurrenceMonth += interval;
            }
        }

        private static DateTime BuildMonthlyOccurrenceStart(DateTime start, int monthsToAdd)
        {
            var targetMonth = start.AddMonths(monthsToAdd);
            var daysInMonth = DateTime.DaysInMonth(targetMonth.Year, targetMonth.Month);
            var day = Math.Min(start.Day, daysInMonth);

            return new DateTime(
                targetMonth.Year,
                targetMonth.Month,
                day,
                start.Hour,
                start.Minute,
                start.Second,
                start.Millisecond,
                DateTimeKind.Utc);
        }

        private static bool IsMatchingWeeklyInterval(DateTime seriesStart, DateTime occurrenceStart, int repeatInterval)
        {
            var startWeekStart = StartOfWeek(seriesStart.Date, DayOfWeek.Sunday);
            var occurrenceWeekStart = StartOfWeek(occurrenceStart.Date, DayOfWeek.Sunday);
            var weeks = (int)((occurrenceWeekStart - startWeekStart).TotalDays / 7);
            return weeks % Math.Max(repeatInterval, 1) == 0;
        }

        private static DateTime StartOfWeek(DateTime date, DayOfWeek startOfWeek)
        {
            var diff = (7 + (date.DayOfWeek - startOfWeek)) % 7;
            return date.AddDays(-diff);
        }

        private static List<DayOfWeek> ParseDaysOfWeekMask(int? mask)
        {
            var result = new List<DayOfWeek>();
            if (!mask.HasValue)
            {
                return result;
            }

            foreach (DayOfWeek day in Enum.GetValues<DayOfWeek>())
            {
                if ((mask.Value & (1 << (int)day)) != 0)
                {
                    result.Add(day);
                }
            }

            return result;
        }

        private static EventOccurrenceResponse BuildOccurrenceResponse(
            EventSeries series,
            DateTime occurrenceKeyStartAt,
            IReadOnlyDictionary<(Guid SeriesId, DateTime ScheduledStartAt), EventOccurrence> overrideLookup)
        {
            var key = (series.Id, NormalizeTimestamp(occurrenceKeyStartAt));
            if (overrideLookup.TryGetValue(key, out var overrideOccurrence))
            {
                var effectiveStartAt = NormalizeTimestamp(overrideOccurrence.OverrideStartAt ?? occurrenceKeyStartAt);
                var defaultEndAt = CalculateScheduledEndAt(occurrenceKeyStartAt, series.DurationMinutes);
                var overrideEndAt = overrideOccurrence.OverrideEndAt;
                var effectiveEndAt = overrideEndAt ?? (overrideOccurrence.OverrideStartAt.HasValue
                    ? CalculateScheduledEndAt(effectiveStartAt, series.DurationMinutes)
                    : defaultEndAt);

                return new EventOccurrenceResponse
                {
                    Id = overrideOccurrence.Id,
                    EventSeriesId = series.Id,
                    Title = overrideOccurrence.OverrideTitle ?? series.Title,
                    Description = overrideOccurrence.OverrideDescription ?? series.Description,
                    ScheduledStartAt = TimeHelper.ToTaiwanOffset(effectiveStartAt),
                    ScheduledEndAt = TimeHelper.ToTaiwanOffset(effectiveEndAt),
                    Participants = (overrideOccurrence.OverrideParticipants ?? series.Participants).ToList(),
                    Status = overrideOccurrence.Status,
                    IsImportant = overrideOccurrence.OverrideIsImportant ?? series.IsImportant,
                    HasOverrides = true
                };
            }

            return new EventOccurrenceResponse
            {
                Id = null,
                EventSeriesId = series.Id,
                Title = series.Title,
                Description = series.Description,
                ScheduledStartAt = TimeHelper.ToTaiwanOffset(occurrenceKeyStartAt),
                ScheduledEndAt = TimeHelper.ToTaiwanOffset(CalculateScheduledEndAt(occurrenceKeyStartAt, series.DurationMinutes)),
                Participants = series.Participants.ToList(),
                Status = EventOccurrenceStatus.Scheduled,
                IsImportant = series.IsImportant,
                HasOverrides = false
            };
        }

        private static DateTime ResolveEffectiveStartAt(
            Guid eventSeriesId,
            DateTime occurrenceKeyStartAt,
            IReadOnlyDictionary<(Guid SeriesId, DateTime ScheduledStartAt), EventOccurrence> overrideLookup)
        {
            var key = (eventSeriesId, NormalizeTimestamp(occurrenceKeyStartAt));
            return overrideLookup.TryGetValue(key, out var overrideOccurrence)
                ? NormalizeTimestamp(overrideOccurrence.OverrideStartAt ?? occurrenceKeyStartAt)
                : NormalizeTimestamp(occurrenceKeyStartAt);
        }

        private static DateTime? CalculateScheduledEndAt(DateTime scheduledStartAt, int? durationMinutes)
        {
            if (!durationMinutes.HasValue || durationMinutes.Value <= 0)
            {
                return null;
            }

            return scheduledStartAt.AddMinutes(durationMinutes.Value);
        }

        private static DateTime? ResolveOverrideStartAt(DateTime? requestedOverrideStartAt, DateTime occurrenceKeyStartAt, DateTime? currentOverrideStartAt)
        {
            if (!requestedOverrideStartAt.HasValue)
            {
                return currentOverrideStartAt;
            }

            var normalizedOverrideStartAt = NormalizeTimestamp(requestedOverrideStartAt.Value);
            return normalizedOverrideStartAt == occurrenceKeyStartAt ? null : normalizedOverrideStartAt;
        }

        private static DateTime? ResolveOverrideEndAt(
            DateTime? requestedOverrideEndAt,
            DateTime effectiveStartAt,
            DateTime occurrenceKeyStartAt,
            int? durationMinutes,
            DateTime? currentOverrideEndAt)
        {
            if (!requestedOverrideEndAt.HasValue)
            {
                return currentOverrideEndAt;
            }

            var normalizedOverrideEndAt = NormalizeTimestamp(requestedOverrideEndAt.Value);
            var defaultEndAt = CalculateScheduledEndAt(occurrenceKeyStartAt, durationMinutes);
            var expectedShiftedEndAt = CalculateScheduledEndAt(NormalizeTimestamp(effectiveStartAt), durationMinutes);

            if (defaultEndAt.HasValue && normalizedOverrideEndAt == defaultEndAt.Value && effectiveStartAt == occurrenceKeyStartAt)
            {
                return null;
            }

            if (expectedShiftedEndAt.HasValue && normalizedOverrideEndAt == expectedShiftedEndAt.Value)
            {
                return null;
            }

            return normalizedOverrideEndAt;
        }

        private static DateTime NormalizeTimestamp(DateTime value)
        {
            var utcValue = value.Kind == DateTimeKind.Utc ? value : value.ToUniversalTime();
            return new DateTime(utcValue.Ticks - (utcValue.Ticks % TimeSpan.TicksPerMillisecond), DateTimeKind.Utc);
        }

        private static DateTime Min(DateTime left, DateTime right)
        {
            return left <= right ? left : right;
        }

        private sealed record ResolvedOccurrence(DateTime OccurrenceKeyStartAt, DateTime CurrentEffectiveStartAt);
        private sealed record UpsertOccurrenceResult(EventSeries Series, EventOccurrence Occurrence, DateTime OccurrenceKeyStartAt, EventOccurrenceStatus PreviousStatus);
    }
}
