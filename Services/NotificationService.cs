using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR;
using SeasonsCare.Api.Config;
using SeasonsCare.Api.DTOs.Common;
using SeasonsCare.Api.DTOs.Notifications;
using SeasonsCare.Api.Exceptions;
using SeasonsCare.Api.Hubs;
using SeasonsCare.Api.Models.Entities;
using SeasonsCare.Api.Models.Enums;
using SeasonsCare.Api.Repositories;

namespace SeasonsCare.Api.Services
{
    public class NotificationService : INotificationService
    {
        private static readonly JsonSerializerOptions PayloadJsonOptions = new(JsonSerializerDefaults.Web);

        private readonly INotificationRepository _notificationRepository;
        private readonly ICareGroupRepository _careGroupRepository;
        private readonly IUserRepository _userRepository;
        private readonly IHubContext<NotificationHub> _hubContext;

        public NotificationService(
            INotificationRepository notificationRepository,
            ICareGroupRepository careGroupRepository,
            IUserRepository userRepository,
            IHubContext<NotificationHub> hubContext)
        {
            _notificationRepository = notificationRepository;
            _careGroupRepository = careGroupRepository;
            _userRepository = userRepository;
            _hubContext = hubContext;
        }

        public async Task<PagedResponse<NotificationResponse>> GetNotificationsAsync(Guid currentUserId, Guid careGroupId, PaginationRequest pagination)
        {
            await CheckMembershipAsync(careGroupId, currentUserId);

            var (data, totalCount) = await _notificationRepository.GetPagedByRecipientAsync(careGroupId, currentUserId, pagination);
            var items = data.Select(MapToResponse).ToList();

            return new PagedResponse<NotificationResponse>(items, totalCount, pagination.Page, pagination.PageSize);
        }

        public async Task<UnreadNotificationCountResponse> GetUnreadCountAsync(Guid currentUserId, Guid careGroupId)
        {
            await CheckMembershipAsync(careGroupId, currentUserId);

            return new UnreadNotificationCountResponse
            {
                UnreadCount = await _notificationRepository.GetUnreadCountAsync(careGroupId, currentUserId)
            };
        }

        public async Task MarkAsReadAsync(Guid currentUserId, Guid careGroupId, Guid notificationId)
        {
            await CheckMembershipAsync(careGroupId, currentUserId);

            var notification = await _notificationRepository.GetByIdAsync(careGroupId, currentUserId, notificationId);
            if (notification == null)
            {
                throw new DomainException("Notification not found.", "NOT_FOUND", 404);
            }

            if (!notification.IsRead)
            {
                var now = GetUtcNowRoundedToMilliseconds();
                notification.IsRead = true;
                notification.ReadAt = now;
                notification.UpdatedAt = now;

                await _notificationRepository.UpdateAsync(notification);
                await _notificationRepository.SaveChangesAsync();
            }

            await PublishUnreadCountUpdatedAsync(careGroupId, currentUserId);
        }

        public async Task<int> MarkAllAsReadAsync(Guid currentUserId, Guid careGroupId)
        {
            await CheckMembershipAsync(careGroupId, currentUserId);

            var unreadNotifications = await _notificationRepository.GetUnreadByRecipientAsync(careGroupId, currentUserId);
            if (unreadNotifications.Count == 0)
            {
                await PublishUnreadCountUpdatedAsync(careGroupId, currentUserId);
                return 0;
            }

            var now = GetUtcNowRoundedToMilliseconds();
            foreach (var notification in unreadNotifications)
            {
                notification.IsRead = true;
                notification.ReadAt = now;
                notification.UpdatedAt = now;
                await _notificationRepository.UpdateAsync(notification);
            }

            await _notificationRepository.SaveChangesAsync();
            await PublishUnreadCountUpdatedAsync(careGroupId, currentUserId);

            return unreadNotifications.Count;
        }

        public async Task NotifyImportantTaskCompletedAsync(Guid actorUserId, Guid careGroupId, Guid eventSeriesId, string title, DateTime scheduledStartAt)
        {
            var actorName = await ResolveActorNameAsync(actorUserId);
            var recipients = await ResolveRecipientsAsync(careGroupId, actorUserId);
            if (recipients.Count == 0)
            {
                return;
            }

            var now = GetUtcNowRoundedToMilliseconds();
            var payloadJson = SerializePayload(new
            {
                eventSeriesId,
                scheduledAt = TimeHelper.ToTaiwanOffset(scheduledStartAt),
                title,
                careGroupId
            });

            var notifications = recipients
                .Select(recipientUserId => new Notification
                {
                    CareGroupId = careGroupId,
                    RecipientUserId = recipientUserId,
                    Type = NotificationType.ImportantTaskCompleted,
                    Title = "重要任務完成",
                    Message = $"{actorName} 已完成重要任務「{title}」。",
                    PayloadJson = payloadJson,
                    IsRead = false,
                    CreatedAt = now,
                    UpdatedAt = now,
                    CreatedBy = actorUserId.ToString()
                })
                .ToList();

            await _notificationRepository.AddRangeAsync(notifications);
            await _notificationRepository.SaveChangesAsync();
            await DispatchCreatedNotificationsAsync(notifications);
        }

        public async Task NotifyExpenseSplitExecutedAsync(Guid actorUserId, Guid careGroupId, Guid splitBatchId, int expenseCount, decimal totalAmount)
        {
            var actorName = await ResolveActorNameAsync(actorUserId);
            var recipients = await ResolveRecipientsAsync(careGroupId, actorUserId);
            if (recipients.Count == 0)
            {
                return;
            }

            var now = GetUtcNowRoundedToMilliseconds();
            var payloadJson = SerializePayload(new
            {
                splitBatchId,
                expenseCount,
                totalAmount,
                careGroupId
            });

            var notifications = recipients
                .Select(recipientUserId => new Notification
                {
                    CareGroupId = careGroupId,
                    RecipientUserId = recipientUserId,
                    Type = NotificationType.ExpenseSplitExecuted,
                    Title = "已執行分帳",
                    Message = $"{actorName} 已完成分帳，共 {expenseCount} 筆，總金額 {totalAmount:0.##}。",
                    PayloadJson = payloadJson,
                    IsRead = false,
                    CreatedAt = now,
                    UpdatedAt = now,
                    CreatedBy = actorUserId.ToString()
                })
                .ToList();

            await _notificationRepository.AddRangeAsync(notifications);
            await _notificationRepository.SaveChangesAsync();
            await DispatchCreatedNotificationsAsync(notifications);
        }

        private async Task CheckMembershipAsync(Guid careGroupId, Guid userId)
        {
            var isMember = await _careGroupRepository.IsMemberAsync(careGroupId, userId);
            if (!isMember)
            {
                throw new DomainException("You are not a member of this care group.", "FORBIDDEN", 403);
            }
        }

        private async Task<List<Guid>> ResolveRecipientsAsync(Guid careGroupId, Guid actorUserId)
        {
            var members = await _careGroupRepository.GetActiveMembersWithUserAsync(careGroupId);
            return members
                .Where(member => member.User != null && member.UserId != actorUserId)
                .Select(member => member.UserId)
                .Distinct()
                .ToList();
        }

        private async Task<string> ResolveActorNameAsync(Guid actorUserId)
        {
            var actor = await _userRepository.GetByIdAsync(actorUserId);
            return actor == null || string.IsNullOrWhiteSpace(actor.Username)
                ? "有成員"
                : actor.Username;
        }

        private async Task DispatchCreatedNotificationsAsync(IEnumerable<Notification> notifications)
        {
            foreach (var notification in notifications)
            {
                var response = MapToResponse(notification);
                await _hubContext.Clients.Group(NotificationHub.GetUserGroup(notification.RecipientUserId))
                    .SendAsync(NotificationHub.NotificationReceivedMethod, response);
                await PublishUnreadCountUpdatedAsync(notification.CareGroupId, notification.RecipientUserId);
            }
        }

        private async Task PublishUnreadCountUpdatedAsync(Guid careGroupId, Guid recipientUserId)
        {
            var unreadCount = await _notificationRepository.GetUnreadCountAsync(careGroupId, recipientUserId);
            await _hubContext.Clients.Group(NotificationHub.GetUserGroup(recipientUserId))
                .SendAsync(NotificationHub.UnreadCountUpdatedMethod, new
                {
                    careGroupId,
                    unreadCount
                });
        }

        private static NotificationResponse MapToResponse(Notification notification)
        {
            return new NotificationResponse
            {
                Id = notification.Id,
                CareGroupId = notification.CareGroupId,
                Type = MapNotificationType(notification.Type),
                Title = notification.Title,
                Message = notification.Message,
                IsRead = notification.IsRead,
                ReadAt = notification.ReadAt.HasValue ? TimeHelper.ToTaiwanOffset(notification.ReadAt.Value) : null,
                CreatedAt = TimeHelper.ToTaiwanOffset(notification.CreatedAt),
                Payload = ParsePayload(notification.PayloadJson)
            };
        }

        private static string MapNotificationType(NotificationType type)
        {
            return type switch
            {
                NotificationType.ImportantTaskCompleted => "important_task_completed",
                NotificationType.ExpenseSplitExecuted => "expense_split_executed",
                _ => "unknown"
            };
        }

        private static string SerializePayload(object payload)
        {
            return JsonSerializer.Serialize(payload, PayloadJsonOptions);
        }

        private static JsonElement? ParsePayload(string? payloadJson)
        {
            if (string.IsNullOrWhiteSpace(payloadJson))
            {
                return null;
            }

            return JsonSerializer.Deserialize<JsonElement>(payloadJson, PayloadJsonOptions);
        }

        private static DateTime GetUtcNowRoundedToMilliseconds()
        {
            return new DateTime(TimeHelper.UtcNow.Ticks - (TimeHelper.UtcNow.Ticks % TimeSpan.TicksPerMillisecond), DateTimeKind.Utc);
        }
    }
}
