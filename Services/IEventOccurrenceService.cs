using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using SeasonsCare.Api.DTOs.EventOccurrences;

namespace SeasonsCare.Api.Services
{
    public interface IEventOccurrenceService
    {
        Task<IReadOnlyList<EventOccurrenceResponse>> GetOccurrencesAsync(Guid currentUserId, Guid careGroupId, DateTime from, DateTime to);
        Task CancelOccurrenceAsync(Guid currentUserId, Guid careGroupId, Guid eventSeriesId, DateTime scheduledStartAt);
        Task CompleteOccurrenceAsync(Guid currentUserId, Guid careGroupId, Guid eventSeriesId, DateTime scheduledStartAt);
    }
}
