using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using SeasonsCare.Api.Config;
using SeasonsCare.Api.DTOs.Common;
using SeasonsCare.Api.DTOs.EventSeries;
using SeasonsCare.Api.Exceptions;
using SeasonsCare.Api.Models.Entities;
using SeasonsCare.Api.Repositories;

namespace SeasonsCare.Api.Services
{
    public class EventSeriesService : IEventSeriesService
    {
        private readonly IEventSeriesRepository _eventSeriesRepository;
        private readonly ICareGroupRepository _careGroupRepository;

        public EventSeriesService(IEventSeriesRepository eventSeriesRepository, ICareGroupRepository careGroupRepository)
        {
            _eventSeriesRepository = eventSeriesRepository;
            _careGroupRepository = careGroupRepository;
        }

        public async Task<PagedResponse<EventSeriesResponse>> GetSeriesAsync(Guid currentUserId, Guid careGroupId, PaginationRequest pagination)
        {
            await CheckMembershipAsync(careGroupId, currentUserId);
            var (data, totalCount) = await _eventSeriesRepository.GetPagedByCareGroupIdAsync(careGroupId, pagination.Page, pagination.PageSize, pagination.Sort);
            var items = data.Select(MapToResponse).ToList();
            return new PagedResponse<EventSeriesResponse>(items, totalCount, pagination.Page, pagination.PageSize);
        }

        public async Task<EventSeriesResponse> GetSeriesByIdAsync(Guid currentUserId, Guid careGroupId, Guid seriesId)
        {
            await CheckMembershipAsync(careGroupId, currentUserId);
            var series = await _eventSeriesRepository.GetByIdAsync(seriesId);
            if (series == null || series.CareGroupId != careGroupId)
            {
                throw new DomainException("Event series not found.", "NOT_FOUND", 404);
            }

            return MapToResponse(series);
        }

        public async Task<EventSeriesResponse> CreateSeriesAsync(Guid currentUserId, Guid careGroupId, CreateEventSeriesRequest request)
        {
            await CheckMembershipAsync(careGroupId, currentUserId);
            var participants = await ValidateParticipantsAsync(careGroupId, request.Participants);
            var now = NormalizeTimestamp(TimeHelper.UtcNow);

            var series = new EventSeries
            {
                Title = request.Title,
                Description = request.Description,
                StartsAt = NormalizeTimestamp(request.StartsAt),
                DurationMinutes = request.DurationMinutes,
                RepeatPattern = request.RepeatPattern,
                RepeatInterval = request.RepeatInterval,
                DaysOfWeekMask = BuildDaysOfWeekMask(request.DaysOfWeek),
                EndType = request.EndType,
                EndAt = request.EndAt.HasValue ? NormalizeTimestamp(request.EndAt.Value) : null,
                OccurrenceCount = request.OccurrenceCount,
                Participants = participants,
                Status = request.Status,
                IsImportant = request.IsImportant,
                CareGroupId = careGroupId,
                CreatedAt = now,
                UpdatedAt = now,
                CreatedBy = currentUserId.ToString()
            };

            await _eventSeriesRepository.AddAsync(series);
            await _eventSeriesRepository.SaveChangesAsync();
            return MapToResponse(series);
        }

        private async Task CheckMembershipAsync(Guid careGroupId, Guid userId)
        {
            var isMember = await _careGroupRepository.IsMemberAsync(careGroupId, userId);
            if (!isMember)
            {
                throw new DomainException("You are not a member of this care group.", "FORBIDDEN", 403);
            }
        }

        private async Task<string[]> ValidateParticipantsAsync(Guid careGroupId, IEnumerable<string>? participants)
        {
            var participantValues = participants?
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Select(x => x.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToArray()
                ?? Array.Empty<string>();

            if (participantValues.Length == 0)
            {
                return Array.Empty<string>();
            }

            if (participantValues.Any(x => !Guid.TryParse(x, out _)))
            {
                throw new DomainException("participants must contain care group member userIds.", "VALIDATION_FAILED", 400);
            }

            var group = await _careGroupRepository.GetByIdAsync(careGroupId);
            if (group == null)
            {
                throw new DomainException("Care group not found.", "NOT_FOUND", 404);
            }

            var memberUserIds = group.Members
                .Where(m => m.DeletedAt == null)
                .Select(m => m.UserId.ToString())
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            if (participantValues.Any(x => !memberUserIds.Contains(x)))
            {
                throw new DomainException("participants can only include userIds of members in this care group.", "VALIDATION_FAILED", 400);
            }

            return participantValues;
        }

        private static int? BuildDaysOfWeekMask(IEnumerable<DayOfWeek>? daysOfWeek)
        {
            if (daysOfWeek == null)
            {
                return null;
            }

            var distinctDays = daysOfWeek.Distinct().ToList();
            if (distinctDays.Count == 0)
            {
                return null;
            }

            var mask = 0;
            foreach (var day in distinctDays)
            {
                mask |= 1 << (int)day;
            }

            return mask;
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

        private static EventSeriesResponse MapToResponse(EventSeries series)
        {
            return new EventSeriesResponse
            {
                Id = series.Id,
                Title = series.Title,
                Description = series.Description,
                StartsAt = TimeHelper.ToTaiwanOffset(series.StartsAt),
                DurationMinutes = series.DurationMinutes,
                RepeatPattern = series.RepeatPattern,
                RepeatInterval = series.RepeatInterval,
                DaysOfWeek = ParseDaysOfWeekMask(series.DaysOfWeekMask),
                EndType = series.EndType,
                EndAt = TimeHelper.ToTaiwanOffset(series.EndAt),
                OccurrenceCount = series.OccurrenceCount,
                Participants = series.Participants.ToList(),
                Status = series.Status,
                IsImportant = series.IsImportant,
                CareGroupId = series.CareGroupId,
                CreatedAt = TimeHelper.ToTaiwanOffset(series.CreatedAt),
                UpdatedAt = TimeHelper.ToTaiwanOffset(series.UpdatedAt),
                CreatedBy = series.CreatedBy
            };
        }

        private static DateTime NormalizeTimestamp(DateTime value)
        {
            var utcValue = value.Kind == DateTimeKind.Utc ? value : value.ToUniversalTime();
            return new DateTime(utcValue.Ticks - (utcValue.Ticks % TimeSpan.TicksPerMillisecond), DateTimeKind.Utc);
        }
    }
}
