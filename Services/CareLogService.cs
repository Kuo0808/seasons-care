using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using SeasonsCare.Api.DTOs.CareLogs;
using SeasonsCare.Api.DTOs.Common;
using SeasonsCare.Api.Exceptions;
using SeasonsCare.Api.Models.Entities;
using SeasonsCare.Api.Repositories;

namespace SeasonsCare.Api.Services
{
    public class CareLogService : ICareLogService
    {
        private readonly ICareLogRepository _careLogRepository;
        private readonly ICareGroupRepository _careGroupRepository;

        public CareLogService(ICareLogRepository careLogRepository, ICareGroupRepository careGroupRepository)
        {
            _careLogRepository = careLogRepository;
            _careGroupRepository = careGroupRepository;
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
                throw new DomainException("Care log not found.", "NOT_FOUND", 404);
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

        public async Task<PagedResponse<CareLogResponse>> GetLogsAsync(Guid currentUserId, Guid careGroupId, PaginationRequest pagination)
        {
            await CheckMembershipAsync(careGroupId, currentUserId);

            // 列表查詢統一走日期區間，供平日記事與時間軸重用。
            var request = pagination.ToDateRangeRequest();
            var (data, totalCount) = await _careLogRepository.GetPagedByCareGroupIdAsync(careGroupId, request);

            var items = data.Select(MapToResponse).ToList();

            return new PagedResponse<CareLogResponse>(items, totalCount, request.Page, request.PageSize);
        }

        public async Task<CareLogResponse> GetLogByIdAsync(Guid currentUserId, Guid careGroupId, Guid logId)
        {
            await CheckMembershipAsync(careGroupId, currentUserId);

            var log = await _careLogRepository.GetByIdAsync(logId);
            if (log == null || log.CareGroupId != careGroupId)
            {
                throw new DomainException("Care log not found.", "NOT_FOUND", 404);
            }

            return MapToResponse(log);
        }

        public async Task<CareLogResponse> CreateLogAsync(Guid currentUserId, Guid careGroupId, CreateCareLogRequest request)
        {
            await CheckMembershipAsync(careGroupId, currentUserId);

            var now = GetUtcNowRoundedToMilliseconds();
            var validatedParticipants = await ValidateParticipantsAsync(careGroupId, request.Participants);

            var log = new CareLog
            {
                Title = request.Title,
                Description = request.Description,
                StartsAt = request.StartsAt ?? now,
                RepeatPattern = request.RepeatPattern,
                Participants = validatedParticipants,
                Status = request.Status,
                IsImportant = request.IsImportant,
                CareGroupId = careGroupId,
                CreatedAt = now,
                UpdatedAt = now,
                CreatedBy = currentUserId.ToString()
            };

            await _careLogRepository.AddAsync(log);
            await _careLogRepository.SaveChangesAsync();

            return MapToResponse(log);
        }

        public async Task<CareLogResponse> UpdateLogAsync(Guid currentUserId, Guid careGroupId, Guid logId, UpdateCareLogRequest request)
        {
            await CheckMembershipAsync(careGroupId, currentUserId);

            var log = await _careLogRepository.GetByIdAsync(logId);
            if (log == null || log.CareGroupId != careGroupId)
            {
                throw new DomainException("Care log not found.", "NOT_FOUND", 404);
            }

            if (!request.UpdatedAt.HasValue || !log.UpdatedAt.HasValue)
            {
                throw new DomainException("Missing updatedAt for concurrency check.", "CONCURRENCY_CONFLICT", 409);
            }

            if (NormalizeTimestamp(request.UpdatedAt.Value) != NormalizeTimestamp(log.UpdatedAt.Value))
            {
                throw new DomainException("Care log has been modified by another request.", "CONCURRENCY_CONFLICT", 409);
            }

            var validatedParticipants = await ValidateParticipantsAsync(careGroupId, request.Participants);

            log.Title = request.Title;
            log.Description = request.Description;
            log.RepeatPattern = request.RepeatPattern;
            log.Participants = validatedParticipants;
            log.Status = request.Status;
            log.IsImportant = request.IsImportant;
            if (request.StartsAt.HasValue)
            {
                log.StartsAt = request.StartsAt.Value;
            }

            log.UpdatedAt = GetUtcNowRoundedToMilliseconds();

            await _careLogRepository.UpdateAsync(log);
            await _careLogRepository.SaveChangesAsync();

            return MapToResponse(log);
        }

        public async Task DeleteLogAsync(Guid currentUserId, Guid careGroupId, Guid logId)
        {
            await CheckMembershipAsync(careGroupId, currentUserId);

            var log = await _careLogRepository.GetByIdAsync(logId);
            if (log == null || log.CareGroupId != careGroupId)
            {
                throw new DomainException("Care log not found.", "NOT_FOUND", 404);
            }

            var now = GetUtcNowRoundedToMilliseconds();
            log.DeletedAt = now;
            log.UpdatedAt = now;

            await _careLogRepository.UpdateAsync(log);
            await _careLogRepository.SaveChangesAsync();
        }

        private static CareLogResponse MapToResponse(CareLog log)
        {
            return new CareLogResponse
            {
                Id = log.Id,
                Title = log.Title,
                Description = log.Description,
                StartsAt = log.StartsAt,
                RepeatPattern = log.RepeatPattern,
                Participants = log.Participants.ToList(),
                Status = log.Status,
                IsImportant = log.IsImportant,
                CareGroupId = log.CareGroupId,
                CreatedAt = log.CreatedAt,
                UpdatedAt = log.UpdatedAt,
                CreatedBy = log.CreatedBy
            };
        }

        private static DateTime GetUtcNowRoundedToMilliseconds()
        {
            return NormalizeTimestamp(DateTime.UtcNow);
        }

        private static DateTime NormalizeTimestamp(DateTime value)
        {
            var utcValue = value.Kind == DateTimeKind.Utc ? value : value.ToUniversalTime();
            return new DateTime(utcValue.Ticks - (utcValue.Ticks % TimeSpan.TicksPerMillisecond), DateTimeKind.Utc);
        }
    }
}
