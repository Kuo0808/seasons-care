using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using SeasonsCare.Api.DTOs.Common;
using SeasonsCare.Api.DTOs.Notifications;
using SeasonsCare.Api.Services;

namespace SeasonsCare.Api.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/care-groups/{careGroupId}/notifications")]
    public class NotificationsController : ControllerBase
    {
        private readonly INotificationService _notificationService;
        private readonly ICurrentUserService _currentUserService;

        public NotificationsController(INotificationService notificationService, ICurrentUserService currentUserService)
        {
            _notificationService = notificationService;
            _currentUserService = currentUserService;
        }

        [HttpGet]
        [ProducesResponseType(typeof(ApiResponse<IEnumerable<NotificationResponse>>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> GetNotifications(Guid careGroupId, [FromQuery] PaginationRequest paginationRequest)
        {
            var currentUserId = _currentUserService.UserId;
            var pagedResult = await _notificationService.GetNotificationsAsync(currentUserId, careGroupId, paginationRequest);
            var response = new ApiResponse<IEnumerable<NotificationResponse>>(
                pagedResult.Items,
                "取得通知列表成功",
                HttpContext.TraceIdentifier,
                pagedResult.Pagination);
            return Ok(response);
        }

        [HttpGet("unread-count")]
        [ProducesResponseType(typeof(ApiResponse<UnreadNotificationCountResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> GetUnreadCount(Guid careGroupId)
        {
            var currentUserId = _currentUserService.UserId;
            var result = await _notificationService.GetUnreadCountAsync(currentUserId, careGroupId);
            var response = new ApiResponse<UnreadNotificationCountResponse>(result, "取得未讀通知數成功", HttpContext.TraceIdentifier);
            return Ok(response);
        }

        [HttpPatch("{notificationId}/read")]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> MarkAsRead(Guid careGroupId, Guid notificationId)
        {
            var currentUserId = _currentUserService.UserId;
            await _notificationService.MarkAsReadAsync(currentUserId, careGroupId, notificationId);
            var response = new ApiResponse<object>(null, "通知已標記為已讀", HttpContext.TraceIdentifier);
            return Ok(response);
        }

        [HttpPatch("read-all")]
        [ProducesResponseType(typeof(ApiResponse<MarkAllNotificationsReadResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> MarkAllAsRead(Guid careGroupId)
        {
            var currentUserId = _currentUserService.UserId;
            var markedCount = await _notificationService.MarkAllAsReadAsync(currentUserId, careGroupId);
            var response = new ApiResponse<MarkAllNotificationsReadResponse>(
                new MarkAllNotificationsReadResponse { MarkedCount = markedCount },
                "通知已全部標記為已讀",
                HttpContext.TraceIdentifier);
            return Ok(response);
        }
    }
}
