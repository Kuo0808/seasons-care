using System;
using System.Threading.Tasks;
using SeasonsCare.Api.DTOs.Common;
using SeasonsCare.Api.DTOs.Notifications;

namespace SeasonsCare.Api.Services
{
    public interface INotificationService
    {
        Task<PagedResponse<NotificationResponse>> GetNotificationsAsync(Guid currentUserId, Guid careGroupId, PaginationRequest pagination);
        Task<UnreadNotificationCountResponse> GetUnreadCountAsync(Guid currentUserId, Guid careGroupId);
        Task MarkAsReadAsync(Guid currentUserId, Guid careGroupId, Guid notificationId);
        Task<int> MarkAllAsReadAsync(Guid currentUserId, Guid careGroupId);
        Task NotifyImportantTaskCompletedAsync(Guid actorUserId, Guid careGroupId, Guid eventSeriesId, string title, DateTime scheduledStartAt);
        Task NotifyExpenseSplitExecutedAsync(Guid actorUserId, Guid careGroupId, Guid splitBatchId, int expenseCount, decimal totalAmount);
    }
}
