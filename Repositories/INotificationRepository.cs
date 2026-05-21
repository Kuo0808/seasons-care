using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using SeasonsCare.Api.DTOs.Common;
using SeasonsCare.Api.Models.Entities;

namespace SeasonsCare.Api.Repositories
{
    public interface INotificationRepository
    {
        Task AddRangeAsync(IEnumerable<Notification> notifications);
        Task<(List<Notification> Data, int TotalCount)> GetPagedByRecipientAsync(Guid careGroupId, Guid recipientUserId, PaginationRequest request);
        Task<Notification?> GetByIdAsync(Guid careGroupId, Guid recipientUserId, Guid notificationId);
        Task<List<Notification>> GetUnreadByRecipientAsync(Guid careGroupId, Guid recipientUserId);
        Task<int> GetUnreadCountAsync(Guid careGroupId, Guid recipientUserId);
        Task UpdateAsync(Notification notification);
        Task SaveChangesAsync();
    }
}
