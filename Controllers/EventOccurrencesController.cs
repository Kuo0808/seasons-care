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
    /// 事件實例 API。
    /// 提供查詢、取消與完成單次事件的能力。
    /// </summary>
    [Authorize]
    [ApiController]
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
        [HttpGet]
        [ProducesResponseType(typeof(ApiResponse<IEnumerable<EventOccurrenceResponse>>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [EndpointSummary("取得事件實例列表")]
        [EndpointDescription("依照 from 與 to 查詢指定照護群組內的事件實例，回傳已展開的單次事件資料。")]
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
        [HttpPost("cancel")]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [EndpointSummary("取消單次事件")]
        [EndpointDescription("依照 eventSeriesId 與 scheduledStartAt 只取消該次事件實例，不影響同系列其他事件。")]
        public async Task<IActionResult> CancelOccurrence(Guid careGroupId, [FromBody] CancelEventOccurrenceRequest request)
        {
            var currentUserId = _currentUserService.UserId;
            await _eventOccurrenceService.CancelOccurrenceAsync(currentUserId, careGroupId, request.EventSeriesId, request.ScheduledStartAt);
            var response = new ApiResponse<object>(null, "取消單次事件成功", HttpContext.TraceIdentifier);
            return Ok(response);
        }

        /// <summary>
        /// 標記單次事件已完成。
        /// </summary>
        [HttpPost("complete")]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [EndpointSummary("標記單次事件完成")]
        [EndpointDescription("依照 eventSeriesId 與 scheduledStartAt 只更新該次事件實例，將狀態標記為 completed，不影響同系列其他事件。")]
        public async Task<IActionResult> CompleteOccurrence(Guid careGroupId, [FromBody] CompleteEventOccurrenceRequest request)
        {
            var currentUserId = _currentUserService.UserId;
            await _eventOccurrenceService.CompleteOccurrenceAsync(currentUserId, careGroupId, request.EventSeriesId, request.ScheduledStartAt);
            var response = new ApiResponse<object>(null, "標記單次事件完成成功", HttpContext.TraceIdentifier);
            return Ok(response);
        }
    }
}
