using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using SeasonsCare.Api.DTOs.Common;
using SeasonsCare.Api.DTOs.HealthRecords.BloodOxygens;
using SeasonsCare.Api.Exceptions;
using SeasonsCare.Api.Models.Entities.HealthRecords;
using SeasonsCare.Api.Repositories;
using SeasonsCare.Api.Repositories.HealthRecords;

namespace SeasonsCare.Api.Services.HealthRecords
{
    // [架構導覽] 商業邏輯層 (Business Logic Layer) - Service
    // 職責：負責執行血氧紀錄的特定領域規則 (Domain Rules)、查核所屬群組權限、進行實體轉換。驗證後方能交由 Repository 存取。
    public class BloodOxygenService : IBloodOxygenService
    {
        private readonly IBloodOxygenRepository _repository;
        private readonly ICareGroupRepository _careGroupRepository;

        public BloodOxygenService(IBloodOxygenRepository repository, ICareGroupRepository careGroupRepository)
        {
            _repository = repository;
            _careGroupRepository = careGroupRepository;
        }

        private async Task ValidateCareGroupAccessAsync(Guid careGroupId, Guid currentUserId)
        {
            var isMember = await _careGroupRepository.IsMemberAsync(careGroupId, currentUserId);
            if (!isMember)
            {
                throw new DomainException("無權限存取該群組的血氧紀錄", "FORBIDDEN", 403);
            }
        }

        public async Task<PagedResponse<BloodOxygenResponse>> GetRecordsAsync(Guid currentUserId, Guid careGroupId, PaginationRequest paginationRequest)
        {
            await ValidateCareGroupAccessAsync(careGroupId, currentUserId);

            var pagedResult = await _repository.GetPagedAsync(careGroupId, paginationRequest);

            var responseItems = pagedResult.Items.Select(MapToResponse).ToList();
            return new PagedResponse<BloodOxygenResponse>(responseItems, pagedResult.Pagination.TotalCount, paginationRequest.Page, paginationRequest.PageSize);
        }

        public async Task<BloodOxygenResponse> GetRecordByIdAsync(Guid currentUserId, Guid careGroupId, Guid recordId)
        {
            await ValidateCareGroupAccessAsync(careGroupId, currentUserId);

            var record = await _repository.GetByIdAsync(careGroupId, recordId);
            if (record == null)
            {
                throw new DomainException("找不到該血氧紀錄", "NOT_FOUND", 404);
            }

            return MapToResponse(record);
        }

        public async Task<BloodOxygenResponse> CreateRecordAsync(Guid currentUserId, Guid careGroupId, CreateBloodOxygenRequest request)
        {
            // 步驟 1：執行前置邏輯校驗與權限審核
            await ValidateCareGroupAccessAsync(careGroupId, currentUserId);

            var now = GetUtcNowRoundedToMilliseconds();

            // 步驟 2：將前端請求 DTO 封裝為標準資料庫實體 Entity
            var record = new BloodOxygenRecord
            {
                CareGroupId = careGroupId,
                SpO2 = request.SpO2,
                Notes = request.Notes,
                RecordDate = request.RecordDate ?? now,
                CreatedAt = now,
                UpdatedAt = now,
                CreatedBy = currentUserId.ToString()
            };

            // 步驟 3：透過 Repository 層保存 Entity
            var created = await _repository.AddAsync(record);
            
            // 步驟 4：進行資料映射 (Map to Response)
            return MapToResponse(created);
        }

        public async Task<BloodOxygenResponse> UpdateRecordAsync(Guid currentUserId, Guid careGroupId, Guid recordId, UpdateBloodOxygenRequest request)
        {
            await ValidateCareGroupAccessAsync(careGroupId, currentUserId);

            var record = await _repository.GetByIdAsync(careGroupId, recordId);
            if (record == null)
            {
                throw new DomainException("找不到該血氧紀錄", "NOT_FOUND", 404);
            }

            if (NormalizeTimestamp(request.UpdatedAt) != NormalizeTimestamp(record.UpdatedAt))
            {
                throw new DomainException("資料已被他人修改，請重新取得最新資料", "CONCURRENCY_CONFLICT", 409);
            }

            record.SpO2 = request.SpO2;
            record.Notes = request.Notes;
            if (request.RecordDate.HasValue)
            {
                record.RecordDate = request.RecordDate.Value;
            }
            record.UpdatedAt = GetUtcNowRoundedToMilliseconds();

            var updated = await _repository.UpdateAsync(record);
            return MapToResponse(updated);
        }

        public async Task DeleteRecordAsync(Guid currentUserId, Guid careGroupId, Guid recordId)
        {
            await ValidateCareGroupAccessAsync(careGroupId, currentUserId);

            var record = await _repository.GetByIdAsync(careGroupId, recordId);
            if (record == null)
            {
                throw new DomainException("找不到該血氧紀錄", "NOT_FOUND", 404);
            }

            var now = GetUtcNowRoundedToMilliseconds();
            record.DeletedAt = now;
            record.UpdatedAt = now;
            await _repository.UpdateAsync(record);
        }

        private static BloodOxygenResponse MapToResponse(BloodOxygenRecord record)
        {
            return new BloodOxygenResponse
            {
                Id = record.Id,
                CareGroupId = record.CareGroupId,
                SpO2 = record.SpO2,
                Notes = record.Notes,
                RecordDate = record.RecordDate,
                CreatedAt = record.CreatedAt,
                UpdatedAt = record.UpdatedAt,
                CreatedBy = record.CreatedBy
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
