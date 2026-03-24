using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using SeasonsCare.Api.DTOs.Common;
using SeasonsCare.Api.DTOs.HealthRecords.Weights;
using SeasonsCare.Api.Exceptions;
using SeasonsCare.Api.Models.Entities.HealthRecords;
using SeasonsCare.Api.Repositories;
using SeasonsCare.Api.Repositories.HealthRecords;

namespace SeasonsCare.Api.Services.HealthRecords
{
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
            await ValidateCareGroupAccessAsync(careGroupId, currentUserId);

            var record = new WeightRecord
            {
                CareGroupId = careGroupId,
                Value = request.Value,
                Notes = request.Notes,
                RecordDate = request.RecordDate ?? DateTime.UtcNow,
                CreatedBy = currentUserId.ToString()
            };

            var created = await _repository.AddAsync(record);
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
            record.UpdatedAt = DateTime.UtcNow;

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

            record.DeletedAt = DateTime.UtcNow;
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
