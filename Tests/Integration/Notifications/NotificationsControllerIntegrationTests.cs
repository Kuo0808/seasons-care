using System;
using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;
using SeasonsCare.Api.DTOs.Common;
using SeasonsCare.Api.DTOs.Notifications;
using SeasonsCare.Api.Exceptions;
using SeasonsCare.Api.Services;
using SeasonsCare.Api.Tests.Shared;
using SeasonsCare.Api.Tests.Shared.Http;

namespace SeasonsCare.Api.Tests.Integration.Notifications;

public class NotificationsControllerIntegrationTests
{
    [Fact]
    public async Task GetNotifications_ReturnsPagedNotifications()
    {
        using var factory = new StubApiFactory<INotificationService>(new StubNotificationService());
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/api/care-groups/aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa/notifications?page=1&pageSize=10");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        using var payload = await JsonResponseHelper.ReadJsonAsync(response);
        var data = payload.RootElement.GetProperty("data");
        Assert.Single(data.EnumerateArray());
        Assert.Equal("important_task_completed", data[0].GetProperty("type").GetString());
        Assert.Equal(1, payload.RootElement.GetProperty("pagination").GetProperty("totalCount").GetInt32());
    }

    [Fact]
    public async Task GetUnreadCount_ReturnsUnreadCount()
    {
        using var factory = new StubApiFactory<INotificationService>(new StubNotificationService());
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/api/care-groups/aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa/notifications/unread-count");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        using var payload = await JsonResponseHelper.ReadJsonAsync(response);
        Assert.Equal(3, payload.RootElement.GetProperty("data").GetProperty("unreadCount").GetInt32());
    }

    [Fact]
    public async Task MarkAsRead_ReturnsNotFound_WhenServiceThrows()
    {
        using var factory = new StubApiFactory<INotificationService>(new StubNotificationService
        {
            MarkAsReadException = new DomainException("missing", "NOT_FOUND", 404)
        });
        using var client = factory.CreateClient();

        var response = await client.PatchAsync("/api/care-groups/aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa/notifications/bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb/read", null);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task MarkAllAsRead_ReturnsMarkedCount()
    {
        using var factory = new StubApiFactory<INotificationService>(new StubNotificationService());
        using var client = factory.CreateClient();

        var response = await client.PatchAsync("/api/care-groups/aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa/notifications/read-all", null);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        using var payload = await JsonResponseHelper.ReadJsonAsync(response);
        Assert.Equal(2, payload.RootElement.GetProperty("data").GetProperty("markedCount").GetInt32());
    }

    private sealed class StubNotificationService : INotificationService
    {
        public Exception? MarkAsReadException { get; init; }

        public Task<PagedResponse<NotificationResponse>> GetNotificationsAsync(Guid currentUserId, Guid careGroupId, PaginationRequest pagination)
        {
            var items = new[]
            {
                new NotificationResponse
                {
                    Id = Guid.NewGuid(),
                    CareGroupId = careGroupId,
                    Type = "important_task_completed",
                    Title = "重要任務完成",
                    Message = "有成員已完成重要任務。",
                    IsRead = false,
                    CreatedAt = new DateTimeOffset(2026, 5, 21, 9, 0, 0, TimeSpan.Zero)
                }
            };

            return Task.FromResult(new PagedResponse<NotificationResponse>(items, 1, pagination.Page, pagination.PageSize));
        }

        public Task<UnreadNotificationCountResponse> GetUnreadCountAsync(Guid currentUserId, Guid careGroupId)
        {
            return Task.FromResult(new UnreadNotificationCountResponse
            {
                UnreadCount = 3
            });
        }

        public Task MarkAsReadAsync(Guid currentUserId, Guid careGroupId, Guid notificationId)
        {
            if (MarkAsReadException != null)
            {
                throw MarkAsReadException;
            }

            return Task.CompletedTask;
        }

        public Task<int> MarkAllAsReadAsync(Guid currentUserId, Guid careGroupId)
        {
            return Task.FromResult(2);
        }

        public Task NotifyImportantTaskCompletedAsync(Guid actorUserId, Guid careGroupId, Guid eventSeriesId, string title, DateTime scheduledStartAt)
            => Task.CompletedTask;

        public Task NotifyExpenseSplitExecutedAsync(Guid actorUserId, Guid careGroupId, Guid splitBatchId, int expenseCount, decimal totalAmount)
            => Task.CompletedTask;
    }
}
