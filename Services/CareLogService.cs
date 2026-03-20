using System;
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
                throw new DomainException("無權存取此 Care Group 的資料", "FORBIDDEN", 403);
            }
        }

        public async Task<PagedResponse<CareLogResponse>> GetLogsAsync(Guid currentUserId, Guid careGroupId, PaginationRequest pagination)
        {
            await CheckMembershipAsync(careGroupId, currentUserId);

            var (data, totalCount) = await _careLogRepository.GetPagedByCareGroupIdAsync(
                careGroupId, 
                pagination.Page, 
                pagination.PageSize, 
                pagination.Sort);

            var items = data.Select(MapToResponse).ToList();

            return new PagedResponse<CareLogResponse>(items, totalCount, pagination.Page, pagination.PageSize);
        }

        public async Task<CareLogResponse> GetLogByIdAsync(Guid currentUserId, Guid careGroupId, Guid logId)
        {
            await CheckMembershipAsync(careGroupId, currentUserId);

            var log = await _careLogRepository.GetByIdAsync(logId);
            if (log == null || log.CareGroupId != careGroupId)
            {
                throw new DomainException("找不到此照護日誌", "NOT_FOUND", 404);
            }

            return MapToResponse(log);
        }

        public async Task<CareLogResponse> CreateLogAsync(Guid currentUserId, Guid careGroupId, CreateCareLogRequest request)
        {
            await CheckMembershipAsync(careGroupId, currentUserId);

            var now = GetUtcNowRoundedToMilliseconds();

            var log = new CareLog
            {
                Title = request.Title,
                Content = request.Content,
                LogType = request.LogType,
                RecordDate = request.RecordDate ?? now,
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
                throw new DomainException("找不到此照護日誌", "NOT_FOUND", 404);
            }

            if (!request.UpdatedAt.HasValue || !log.UpdatedAt.HasValue)
            {
                throw new DomainException("缺少併發控制資訊，請重新整理後再試", "CONCURRENCY_CONFLICT", 409);
            }

            if (NormalizeTimestamp(request.UpdatedAt.Value) != NormalizeTimestamp(log.UpdatedAt.Value))
            {
                throw new DomainException("資料已被修改，請重新整理後再試", "CONCURRENCY_CONFLICT", 409);
            }

            log.Title = request.Title;
            log.Content = request.Content;
            log.LogType = request.LogType;
            if (request.RecordDate.HasValue)
            {
                log.RecordDate = request.RecordDate.Value;
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
                throw new DomainException("找不到此照護日誌", "NOT_FOUND", 404);
            }

            var now = GetUtcNowRoundedToMilliseconds();
            log.DeletedAt = now; // Soft delete
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
                Content = log.Content,
                LogType = log.LogType,
                RecordDate = log.RecordDate,
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
