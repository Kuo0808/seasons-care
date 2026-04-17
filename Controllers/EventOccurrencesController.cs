using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using SeasonsCare.Api.DTOs.Common;
using SeasonsCare.Api.DTOs.EventOccurrences;
using SeasonsCare.Api.Services;

namespace SeasonsCare.Api.Controllers
{
    /// <summary>
    /// 事件實例 API（進階：直接操作單次覆寫）。
    /// 提供查詢、取消、完成、更新單次事件，以及清除單次覆寫回復系列預設的能力。
    /// 前端一般頁面串接請優先使用 /events Facade（FME-2 查列表、FME-5 改狀態、FME-6 查單次）；此模組保留給特殊整合情境使用。
    /// </summary>
    [Authorize]
    [ApiController]
    [Tags("Event Occurrences (進階：直接操作單次覆寫)")]
    [Route("api/care-groups/{careGroupId}/event-occurrences")]
    public class EventOccurrencesController : ControllerBase
    {
        private readonly IEventOccurrenceService _eventOccurrenceService;
        private readonly ICurrentUserService _currentUserService;

        public EventOccurrencesController(IEventOccurrenceService eventOccurrenceService, ICurrentUserService currentUserService)
        {
            _eventOccurrenceService = eventOccurrenceService;
            _currentUserService = currentUserService;
        }

        /// <summary>
        /// 取得指定區間內的事件實例。
        /// </summary>
        /// <param name="careGroupId">照護群組 ID。</param>
        /// <param name="request">查詢區間，from 與 to 皆必填，回傳結果已套用單次覆寫。</param>
        [HttpGet]
        [ProducesResponseType(typeof(ApiResponse<IEnumerable<EventOccurrenceResponse>>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [EndpointSummary("取得事件實例")]
        [EndpointDescription("依 from 與 to 查詢區間內的事件實例，回傳已套用單次覆寫後的結果。")]
        public async Task<IActionResult> GetOccurrences(Guid careGroupId, [FromQuery] GetEventOccurrencesRequest request)
        {
            var currentUserId = _currentUserService.UserId;
            var items = await _eventOccurrenceService.GetOccurrencesAsync(currentUserId, careGroupId, request.From, request.To);
            var response = new ApiResponse<IEnumerable<EventOccurrenceResponse>>(items, "取得事件實例成功", HttpContext.TraceIdentifier);
            return Ok(response);
        }

        /// <summary>
        /// 取消單次事件。
        /// </summary>
        /// <param name="careGroupId">照護群組 ID。</param>
        /// <param name="request">以 eventSeriesId 與 scheduledStartAt 指定要取消的單次事件。</param>
        [HttpPost("cancel")]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [EndpointSummary("取消單次事件")]
        [EndpointDescription("依 eventSeriesId 與 scheduledStartAt 將單次事件標記為 cancelled。")]
        public async Task<IActionResult> CancelOccurrence(Guid careGroupId, [FromBody] CancelEventOccurrenceRequest request)
        {
            var currentUserId = _currentUserService.UserId;
            await _eventOccurrenceService.CancelOccurrenceAsync(currentUserId, careGroupId, request.EventSeriesId, request.ScheduledStartAt);
            var response = new ApiResponse<object>(null, "取消單次事件成功", HttpContext.TraceIdentifier);
            return Ok(response);
        }

        /// <summary>
        /// 標記單次事件完成。
        /// </summary>
        /// <param name="careGroupId">照護群組 ID。</param>
        /// <param name="request">以 eventSeriesId 與 scheduledStartAt 指定要標記完成的單次事件。</param>
        [HttpPost("complete")]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [EndpointSummary("完成單次事件")]
        [EndpointDescription("依 eventSeriesId 與 scheduledStartAt 將單次事件標記為 completed。")]
        public async Task<IActionResult> CompleteOccurrence(Guid careGroupId, [FromBody] CompleteEventOccurrenceRequest request)
        {
            var currentUserId = _currentUserService.UserId;
            await _eventOccurrenceService.CompleteOccurrenceAsync(currentUserId, careGroupId, request.EventSeriesId, request.ScheduledStartAt);
            var response = new ApiResponse<object>(null, "完成單次事件成功", HttpContext.TraceIdentifier);
            return Ok(response);
        }

        /// <summary>
        /// 更新單次事件。
        /// </summary>
        /// <param name="careGroupId">照護群組 ID。</param>
        /// <param name="request">指定單次事件後，可一次更新標題、描述、時間、參與者、狀態與重要性。</param>
        [HttpPatch]
        [ProducesResponseType(typeof(ApiResponse<EventOccurrenceResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [EndpointSummary("更新單次事件")]
        [EndpointDescription("更新單次事件的時間、內容、參與者、狀態與重要性，不影響原始 series 規則。")]
        public async Task<IActionResult> UpdateOccurrence(Guid careGroupId, [FromBody] UpdateEventOccurrenceRequest request)
        {
            var currentUserId = _currentUserService.UserId;
            var item = await _eventOccurrenceService.UpdateOccurrenceAsync(currentUserId, careGroupId, request);
            var response = new ApiResponse<EventOccurrenceResponse>(item, "更新事件實例成功", HttpContext.TraceIdentifier);
            return Ok(response);
        }

        /// <summary>
        /// 清除單次事件覆寫。
        /// </summary>
        /// <param name="careGroupId">照護群組 ID。</param>
        /// <param name="request">以 eventSeriesId 與 scheduledStartAt 指定要移除覆寫的單次事件，清除後會回復成系列預設值。</param>
        [HttpDelete("override")]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [EndpointSummary("清除單次覆寫")]
        [EndpointDescription("刪除單次事件覆寫，回復成 series 預設內容。")]
        public async Task<IActionResult> ClearOccurrenceOverride(Guid careGroupId, [FromBody] ClearEventOccurrenceOverrideRequest request)
        {
            var currentUserId = _currentUserService.UserId;
            await _eventOccurrenceService.ClearOccurrenceOverrideAsync(currentUserId, careGroupId, request.EventSeriesId, request.ScheduledStartAt);
            var response = new ApiResponse<object>(null, "清除事件實例覆寫成功", HttpContext.TraceIdentifier);
            return Ok(response);
        }
    }
}
