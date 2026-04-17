using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using SeasonsCare.Api.DTOs.Common;
using SeasonsCare.Api.DTOs.EventSeries;

namespace SeasonsCare.Api.Services
{
    public interface IEventSeriesService
    {
        Task<PagedResponse<EventSeriesResponse>> GetSeriesAsync(Guid currentUserId, Guid careGroupId, PaginationRequest pagination);
        Task<IReadOnlyList<EventSeriesResponse>> GetAllSeriesAsync(Guid currentUserId, Guid careGroupId);
        Task<EventSeriesResponse> GetSeriesByIdAsync(Guid currentUserId, Guid careGroupId, Guid seriesId);
        Task<EventSeriesResponse> CreateSeriesAsync(Guid currentUserId, Guid careGroupId, CreateEventSeriesRequest request);
        Task<EventSeriesResponse> UpdateSeriesAsync(Guid currentUserId, Guid careGroupId, Guid seriesId, UpdateEventSeriesRequest request);
        Task DeleteSeriesAsync(Guid currentUserId, Guid careGroupId, Guid seriesId);
    }
}
