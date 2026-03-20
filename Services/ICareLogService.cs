using System;
using System.Threading.Tasks;
using SeasonsCare.Api.DTOs.CareLogs;
using SeasonsCare.Api.DTOs.Common;

namespace SeasonsCare.Api.Services
{
    public interface ICareLogService
    {
        Task<PagedResponse<CareLogResponse>> GetLogsAsync(Guid currentUserId, Guid careGroupId, PaginationRequest pagination);
        Task<CareLogResponse> GetLogByIdAsync(Guid currentUserId, Guid careGroupId, Guid logId);
        Task<CareLogResponse> CreateLogAsync(Guid currentUserId, Guid careGroupId, CreateCareLogRequest request);
        Task<CareLogResponse> UpdateLogAsync(Guid currentUserId, Guid careGroupId, Guid logId, UpdateCareLogRequest request);
        Task DeleteLogAsync(Guid currentUserId, Guid careGroupId, Guid logId);
    }
}
