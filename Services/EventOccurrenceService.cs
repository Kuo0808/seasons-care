using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
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

        public EventOccurrenceService(
            IEventSeriesRepository eventSeriesRepository,
            IEventOccurrenceRepository eventOccurrenceRepository,
            ICareGroupRepository careGroupRepository)
        {
            _eventSeriesRepository = eventSeriesRepository;
            _eventOccurrenceRepository = eventOccurrenceRepository;
            _careGroupRepository = careGroupRepository;
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
            var overrideLookup = overrides.ToDictionary(x => (x.EventSeriesId, NormalizeTimestamp(x.ScheduledStartAt)));

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

        public async Task CancelOccurrenceAsync(Guid currentUserId, Guid careGroupId, Guid eventSeriesId, DateTime scheduledStartAt)
        {
            await CheckMembershipAsync(careGroupId, currentUserId);

            var series = await _eventSeriesRepository.GetByIdAsync(eventSeriesId);
            if (series == null || series.CareGroupId != careGroupId)
            {
                throw new DomainException("Event series not found.", "NOT_FOUND", 404);
            }

            var normalizedStartAt = NormalizeTimestamp(scheduledStartAt);
            var occurrenceExists = ExpandSeriesOccurrences(
                    series,
                    normalizedStartAt,
                    normalizedStartAt,
                    new Dictionary<(Guid SeriesId, DateTime ScheduledStartAt), EventOccurrence>())
                .Any();

            if (!occurrenceExists)
            {
                throw new DomainException("Event occurrence not found in this series.", "NOT_FOUND", 404);
            }

            var existing = await _eventOccurrenceRepository.GetBySeriesIdAndScheduledStartAtAsync(eventSeriesId, normalizedStartAt);
            var now = NormalizeTimestamp(DateTime.UtcNow);

            if (existing == null)
            {
                var created = new EventOccurrence
                {
                    EventSeriesId = eventSeriesId,
                    CareGroupId = careGroupId,
                    ScheduledStartAt = normalizedStartAt,
                    ScheduledEndAt = CalculateScheduledEndAt(normalizedStartAt, series.DurationMinutes),
                    Status = EventOccurrenceStatus.Cancelled,
                    CreatedAt = now,
                    UpdatedAt = now,
                    CreatedBy = currentUserId.ToString()
                };

                await _eventOccurrenceRepository.AddAsync(created);
            }
            else
            {
                existing.Status = EventOccurrenceStatus.Cancelled;
                existing.UpdatedAt = now;
                await _eventOccurrenceRepository.UpdateAsync(existing);
            }

            await _eventOccurrenceRepository.SaveChangesAsync();
        }

        private async Task CheckMembershipAsync(Guid careGroupId, Guid userId)
        {
            var isMember = await _careGroupRepository.IsMemberAsync(careGroupId, userId);
            if (!isMember)
            {
                throw new DomainException("You are not a member of this care group.", "FORBIDDEN", 403);
            }
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
                EventRepeatPattern.Weekly => ExpandWeekly(series, from, to, overrideLookup),
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

                if (scheduledStartAt < from)
                {
                    continue;
                }

                if (scheduledStartAt > to)
                {
                    yield break;
                }

                yield return BuildOccurrenceResponse(series, scheduledStartAt, overrideLookup);
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

            var allowedDays = ParseDaysOfWeekMask(series.DaysOfWeekMask);
            if (allowedDays.Count == 0)
            {
                allowedDays.Add(start.DayOfWeek);
            }

            var currentDate = start.Date;
            while (currentDate <= effectiveEnd.Date)
            {
                var occurrenceStart = new DateTime(
                    currentDate.Year,
                    currentDate.Month,
                    currentDate.Day,
                    start.Hour,
                    start.Minute,
                    start.Second,
                    start.Millisecond,
                    DateTimeKind.Utc);

                if (occurrenceStart >= start &&
                    allowedDays.Contains(occurrenceStart.DayOfWeek) &&
                    IsMatchingWeeklyInterval(start, occurrenceStart, series.RepeatInterval))
                {
                    yield return occurrenceStart;
                }

                currentDate = currentDate.AddDays(1);
            }
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
            DateTime scheduledStartAt,
            IReadOnlyDictionary<(Guid SeriesId, DateTime ScheduledStartAt), EventOccurrence> overrideLookup)
        {
            var key = (series.Id, NormalizeTimestamp(scheduledStartAt));
            if (overrideLookup.TryGetValue(key, out var overrideOccurrence))
            {
                return new EventOccurrenceResponse
                {
                    Id = overrideOccurrence.Id,
                    EventSeriesId = series.Id,
                    Title = overrideOccurrence.OverrideTitle ?? series.Title,
                    Description = overrideOccurrence.OverrideDescription ?? series.Description,
                    ScheduledStartAt = overrideOccurrence.ScheduledStartAt,
                    ScheduledEndAt = overrideOccurrence.ScheduledEndAt ?? CalculateScheduledEndAt(scheduledStartAt, series.DurationMinutes),
                    Participants = (overrideOccurrence.OverrideParticipants ?? series.Participants).ToList(),
                    Status = overrideOccurrence.Status,
                    IsImportant = overrideOccurrence.IsImportantOverride || series.IsImportant,
                    HasOverrides = true
                };
            }

            return new EventOccurrenceResponse
            {
                Id = null,
                EventSeriesId = series.Id,
                Title = series.Title,
                Description = series.Description,
                ScheduledStartAt = scheduledStartAt,
                ScheduledEndAt = CalculateScheduledEndAt(scheduledStartAt, series.DurationMinutes),
                Participants = series.Participants.ToList(),
                Status = EventOccurrenceStatus.Scheduled,
                IsImportant = series.IsImportant,
                HasOverrides = false
            };
        }

        private static DateTime? CalculateScheduledEndAt(DateTime scheduledStartAt, int? durationMinutes)
        {
            if (!durationMinutes.HasValue || durationMinutes.Value <= 0)
            {
                return null;
            }

            return scheduledStartAt.AddMinutes(durationMinutes.Value);
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
    }
}
