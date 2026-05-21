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
    /// <summary>
    /// 通知中心 API。
    /// 提供通知列表、未讀數、單筆已讀與全部已讀功能，供前端通知中心與 badge 使用。
    /// </summary>
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

        /// <summary>
        /// 取得目前使用者在指定照護群組中的通知列表。
        /// </summary>
        /// <param name="careGroupId">照護群組 ID。</param>
        /// <param name="paginationRequest">分頁參數，支援 page、pageSize、sort；預設依建立時間新到舊排序。</param>
        [HttpGet]
        [ProducesResponseType(typeof(ApiResponse<IEnumerable<NotificationResponse>>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [EndpointSummary("取得通知列表")]
        [EndpointDescription("取得目前登入使用者於指定 careGroup 的通知列表。回傳內容包含通知類型、標題、訊息、payload、已讀狀態與建立時間，供前端通知中心顯示。")]
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

        /// <summary>
        /// 取得目前使用者在指定照護群組中的未讀通知數量。
        /// </summary>
        /// <param name="careGroupId">照護群組 ID。</param>
        [HttpGet("unread-count")]
        [ProducesResponseType(typeof(ApiResponse<UnreadNotificationCountResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [EndpointSummary("取得未讀通知數")]
        [EndpointDescription("取得目前登入使用者於指定 careGroup 的未讀通知數量，供前端顯示 badge 或紅點。")]
        public async Task<IActionResult> GetUnreadCount(Guid careGroupId)
        {
            var currentUserId = _currentUserService.UserId;
            var result = await _notificationService.GetUnreadCountAsync(currentUserId, careGroupId);
            var response = new ApiResponse<UnreadNotificationCountResponse>(result, "取得未讀通知數成功", HttpContext.TraceIdentifier);
            return Ok(response);
        }

        /// <summary>
        /// 將指定通知標記為已讀。
        /// </summary>
        /// <param name="careGroupId">照護群組 ID。</param>
        /// <param name="notificationId">通知 ID。</param>
        [HttpPatch("{notificationId}/read")]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [EndpointSummary("標記單筆通知為已讀")]
        [EndpointDescription("將指定通知標記為已讀。僅允許目前登入使用者操作自己在該 careGroup 下收到的通知。")]
        public async Task<IActionResult> MarkAsRead(Guid careGroupId, Guid notificationId)
        {
            var currentUserId = _currentUserService.UserId;
            await _notificationService.MarkAsReadAsync(currentUserId, careGroupId, notificationId);
            var response = new ApiResponse<object>(null, "通知已標記為已讀", HttpContext.TraceIdentifier);
            return Ok(response);
        }

        /// <summary>
        /// 將目前使用者在指定照護群組中的所有未讀通知標記為已讀。
        /// </summary>
        /// <param name="careGroupId">照護群組 ID。</param>
        [HttpPatch("read-all")]
        [ProducesResponseType(typeof(ApiResponse<MarkAllNotificationsReadResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [EndpointSummary("全部通知標記為已讀")]
        [EndpointDescription("將目前登入使用者於指定 careGroup 的所有未讀通知一次標記為已讀，回傳本次更新筆數。")]
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
