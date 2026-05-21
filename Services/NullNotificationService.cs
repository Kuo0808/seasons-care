using System;
using System.Threading.Tasks;
using SeasonsCare.Api.DTOs.Common;
using SeasonsCare.Api.DTOs.Notifications;

namespace SeasonsCare.Api.Services
{
    public sealed class NullNotificationService : INotificationService
    {
        public static NullNotificationService Instance { get; } = new();

        private NullNotificationService()
        {
        }

        public Task<PagedResponse<NotificationResponse>> GetNotificationsAsync(Guid currentUserId, Guid careGroupId, PaginationRequest pagination)
        {
            return Task.FromResult(new PagedResponse<NotificationResponse>(Array.Empty<NotificationResponse>(), 0, pagination.Page, pagination.PageSize));
        }

        public Task<UnreadNotificationCountResponse> GetUnreadCountAsync(Guid currentUserId, Guid careGroupId)
        {
            return Task.FromResult(new UnreadNotificationCountResponse());
        }

        public Task MarkAsReadAsync(Guid currentUserId, Guid careGroupId, Guid notificationId) => Task.CompletedTask;

        public Task<int> MarkAllAsReadAsync(Guid currentUserId, Guid careGroupId) => Task.FromResult(0);

        public Task NotifyImportantTaskCompletedAsync(Guid actorUserId, Guid careGroupId, Guid eventSeriesId, string title, DateTime scheduledStartAt) => Task.CompletedTask;

        public Task NotifyExpenseSplitExecutedAsync(Guid actorUserId, Guid careGroupId, Guid splitBatchId, int expenseCount, decimal totalAmount) => Task.CompletedTask;
    }
}
