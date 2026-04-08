using System;
using System.Threading.Tasks;
using SeasonsCare.Api.DTOs.Common;
using SeasonsCare.Api.DTOs.EventSeries;

namespace SeasonsCare.Api.Services
{
    public interface IEventSeriesService
    {
        Task<PagedResponse<EventSeriesResponse>> GetSeriesAsync(Guid currentUserId, Guid careGroupId, PaginationRequest pagination);
        Task<EventSeriesResponse> GetSeriesByIdAsync(Guid currentUserId, Guid careGroupId, Guid seriesId);
        Task<EventSeriesResponse> CreateSeriesAsync(Guid currentUserId, Guid careGroupId, CreateEventSeriesRequest request);
    }
}
