using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using SeasonsCare.Api.DTOs.Common;
using SeasonsCare.Api.DTOs.BloodPressures;
using SeasonsCare.Api.Exceptions;
using SeasonsCare.Api.Models.Entities;
using SeasonsCare.Api.Repositories;

namespace SeasonsCare.Api.Services
{
    public class BloodPressureService : IBloodPressureService
    {
        private readonly IBloodPressureRepository _repository;
        private readonly ICareGroupRepository _careGroupRepository;

        public BloodPressureService(IBloodPressureRepository repository, ICareGroupRepository careGroupRepository)
        {
            _repository = repository;
            _careGroupRepository = careGroupRepository;
        }

        private async Task ValidateCareGroupAccessAsync(Guid careGroupId, Guid currentUserId)
        {
            var isMember = await _careGroupRepository.IsMemberAsync(careGroupId, currentUserId);
            if (!isMember)
            {
                throw new DomainException("無權限存取該群組的血壓紀錄", "FORBIDDEN", 403);
            }
        }

        public async Task<PagedResponse<BloodPressureResponse>> GetRecordsAsync(Guid currentUserId, Guid careGroupId, PaginationRequest paginationRequest)
        {
            await ValidateCareGroupAccessAsync(careGroupId, currentUserId);

            var pagedResult = await _repository.GetPagedAsync(careGroupId, paginationRequest);

            var responseItems = pagedResult.Items.Select(MapToResponse).ToList();
            return new PagedResponse<BloodPressureResponse>(responseItems, pagedResult.Pagination.TotalCount, paginationRequest.Page, paginationRequest.PageSize);
        }

        public async Task<BloodPressureResponse> GetRecordByIdAsync(Guid currentUserId, Guid careGroupId, Guid recordId)
        {
            await ValidateCareGroupAccessAsync(careGroupId, currentUserId);

            var record = await _repository.GetByIdAsync(careGroupId, recordId);
            if (record == null)
            {
                throw new DomainException("找不到該血壓紀錄", "NOT_FOUND", 404);
            }

            return MapToResponse(record);
        }

        public async Task<BloodPressureResponse> CreateRecordAsync(Guid currentUserId, Guid careGroupId, CreateBloodPressureRequest request)
        {
            await ValidateCareGroupAccessAsync(careGroupId, currentUserId);

            var record = new BloodPressureRecord
            {
                CareGroupId = careGroupId,
                Systolic = request.Systolic,
                Diastolic = request.Diastolic,
                Notes = request.Notes,
                RecordDate = request.RecordDate ?? DateTime.UtcNow,
                CreatedBy = currentUserId
            };

            var created = await _repository.AddAsync(record);
            return MapToResponse(created);
        }

        public async Task<BloodPressureResponse> UpdateRecordAsync(Guid currentUserId, Guid careGroupId, Guid recordId, UpdateBloodPressureRequest request)
        {
            await ValidateCareGroupAccessAsync(careGroupId, currentUserId);

            var record = await _repository.GetByIdAsync(careGroupId, recordId);
            if (record == null)
            {
                throw new DomainException("找不到該血壓紀錄", "NOT_FOUND", 404);
            }

            if (record.UpdatedAt.ToString("O") != request.UpdatedAt.ToString("O"))
            {
                throw new DomainException("資料已被他人修改，請重新取得最新資料", "CONCURRENCY_CONFLICT", 409);
            }

            record.Systolic = request.Systolic;
            record.Diastolic = request.Diastolic;
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
                throw new DomainException("找不到該血壓紀錄", "NOT_FOUND", 404);
            }

            record.DeletedAt = DateTime.UtcNow;
            await _repository.UpdateAsync(record);
        }

        private static BloodPressureResponse MapToResponse(BloodPressureRecord record)
        {
            return new BloodPressureResponse
            {
                Id = record.Id,
                CareGroupId = record.CareGroupId,
                Systolic = record.Systolic,
                Diastolic = record.Diastolic,
                Notes = record.Notes,
                RecordDate = record.RecordDate,
                CreatedAt = record.CreatedAt,
                UpdatedAt = record.UpdatedAt,
                CreatedBy = record.CreatedBy
            };
        }
    }
}
