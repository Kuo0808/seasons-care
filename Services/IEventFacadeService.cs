using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using SeasonsCare.Api.DTOs.Events;

namespace SeasonsCare.Api.Services
{
    /// <summary>
    /// 對前端統一的事件 Facade（對應 FME-1 ~ FME-6）。
    /// 底層轉呼叫 IEventSeriesService + IEventOccurrenceService，本身不含領域邏輯。
    /// </summary>
    public interface IEventFacadeService
    {
        // FME-1
        Task<EventResponse> CreateEventAsync(Guid currentUserId, Guid careGroupId, CreateEventRequest request);

        // FME-2
        Task<IReadOnlyList<EventOccurrenceItem>> GetEventsAsync(Guid currentUserId, Guid careGroupId, GetEventsRequest request);

        // FME-3
        Task<EventResponse> UpdateEventAsync(Guid currentUserId, Guid careGroupId, Guid eventId, UpdateEventRequest request);

        // FME-4
        Task DeleteEventAsync(Guid currentUserId, Guid careGroupId, Guid eventId);

        // FME-5
        Task UpdateInstanceAsync(Guid currentUserId, Guid careGroupId, Guid eventId, UpdateInstanceRequest request);

        // FME-6
        Task<EventOccurrenceItem> GetInstanceStatusAsync(Guid currentUserId, Guid careGroupId, Guid eventId, DateTime scheduledAt);
    }
}
