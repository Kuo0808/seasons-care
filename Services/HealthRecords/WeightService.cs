using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using SeasonsCare.Api.Config;
using SeasonsCare.Api.DTOs.Common;
using SeasonsCare.Api.DTOs.HealthRecords.Weights;
using SeasonsCare.Api.Exceptions;
using SeasonsCare.Api.Models.Entities.HealthRecords;
using SeasonsCare.Api.Repositories;
using SeasonsCare.Api.Repositories.HealthRecords;

namespace SeasonsCare.Api.Services.HealthRecords
{
    // [架構導覽] 商業邏輯層 (Business Logic Layer) - Service
    // 職責：負責執行體重紀錄的特定領域規則 (Domain Rules)、查核所屬群組權限、進行實體轉換。驗證後方能交由 Repository 存取。
    public class WeightService : IWeightService
    {
        private readonly IWeightRepository _repository;
        private readonly ICareGroupRepository _careGroupRepository;

        public WeightService(IWeightRepository repository, ICareGroupRepository careGroupRepository)
        {
            _repository = repository;
            _careGroupRepository = careGroupRepository;
        }

        private async Task ValidateCareGroupAccessAsync(Guid careGroupId, Guid currentUserId)
        {
            var isMember = await _careGroupRepository.IsMemberAsync(careGroupId, currentUserId);
            if (!isMember)
            {
                throw new DomainException("無權限存取該群組的體重紀錄", "FORBIDDEN", 403);
            }
        }

        public async Task<PagedResponse<WeightResponse>> GetRecordsAsync(Guid currentUserId, Guid careGroupId, PaginationRequest paginationRequest)
        {
            await ValidateCareGroupAccessAsync(careGroupId, currentUserId);

            var pagedResult = await _repository.GetPagedAsync(careGroupId, paginationRequest);

            var responseItems = pagedResult.Items.Select(MapToResponse).ToList();
            return new PagedResponse<WeightResponse>(responseItems, pagedResult.Pagination.TotalCount, paginationRequest.Page, paginationRequest.PageSize);
        }

        public async Task<WeightResponse> GetRecordByIdAsync(Guid currentUserId, Guid careGroupId, Guid recordId)
        {
            await ValidateCareGroupAccessAsync(careGroupId, currentUserId);

            var record = await _repository.GetByIdAsync(careGroupId, recordId);
            if (record == null)
            {
                throw new DomainException("找不到該體重紀錄", "NOT_FOUND", 404);
            }

            return MapToResponse(record);
        }

        public async Task<WeightResponse> CreateRecordAsync(Guid currentUserId, Guid careGroupId, CreateWeightRequest request)
        {
            // 步驟 1：執行前置邏輯校驗與權限審核
            await ValidateCareGroupAccessAsync(careGroupId, currentUserId);

            // 步驟 2：將前端請求 DTO 封裝為標準資料庫實體 Entity
            var record = new WeightRecord
            {
                CareGroupId = careGroupId,
                Value = request.Value,
                Notes = request.Notes,
                RecordDate = request.RecordDate ?? TimeHelper.Now,
                CreatedBy = currentUserId.ToString()
            };

            // 步驟 3：透過 Repository 層保存 Entity，並藉由 Entity Framework 取得寫入後生成的完整資料 (如遞增 ID)
            var created = await _repository.AddAsync(record);
            
            // 步驟 4：進行資料映射 (Map to Response)，不對外曝露真實實體
            return MapToResponse(created);
        }

        public async Task<WeightResponse> UpdateRecordAsync(Guid currentUserId, Guid careGroupId, Guid recordId, UpdateWeightRequest request)
        {
            await ValidateCareGroupAccessAsync(careGroupId, currentUserId);

            var record = await _repository.GetByIdAsync(careGroupId, recordId);
            if (record == null)
            {
                throw new DomainException("找不到該體重紀錄", "NOT_FOUND", 404);
            }

            if (record.UpdatedAt.ToString("O") != request.UpdatedAt.ToString("O"))
            {
                throw new DomainException("資料已被他人修改，請重新取得最新資料", "CONCURRENCY_CONFLICT", 409);
            }

            record.Value = request.Value;
            record.Notes = request.Notes;
            if (request.RecordDate.HasValue)
            {
                record.RecordDate = request.RecordDate.Value;
            }
            record.UpdatedAt = TimeHelper.Now;

            var updated = await _repository.UpdateAsync(record);
            return MapToResponse(updated);
        }

        public async Task DeleteRecordAsync(Guid currentUserId, Guid careGroupId, Guid recordId)
        {
            await ValidateCareGroupAccessAsync(careGroupId, currentUserId);

            var record = await _repository.GetByIdAsync(careGroupId, recordId);
            if (record == null)
            {
                throw new DomainException("找不到該體重紀錄", "NOT_FOUND", 404);
            }

            record.DeletedAt = TimeHelper.Now;
            await _repository.UpdateAsync(record);
        }

        private static WeightResponse MapToResponse(WeightRecord record)
        {
            return new WeightResponse
            {
                Id = record.Id,
                CareGroupId = record.CareGroupId,
                Value = record.Value,
                Notes = record.Notes,
                RecordDate = record.RecordDate,
                CreatedAt = record.CreatedAt,
                UpdatedAt = record.UpdatedAt,
                CreatedBy = record.CreatedBy
            };
        }
    }
}
