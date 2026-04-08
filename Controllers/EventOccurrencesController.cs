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
    /// 這個模組用來查詢某時間區間內的事件實例，以及處理單次取消等例外操作。
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
        /// 查詢指定時間區間內的事件實例。
        /// </summary>
        /// <param name="careGroupId">照護群組 ID。</param>
        /// <param name="request">查詢區間。from 與 to 為必填，回傳的是系列事件展開後的實例清單。</param>
        [HttpGet]
        [ProducesResponseType(typeof(ApiResponse<IEnumerable<EventOccurrenceResponse>>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [EndpointSummary("查詢事件實例區間")]
        [EndpointDescription("查詢指定照護群組在某段時間區間內的事件實例。前端需在 query string 提供 from 與 to。系統會根據事件系列規則，自動展開這段時間內應出現的每一筆事件。這一版支援單次事件與每週重複事件。")]
        public async Task<IActionResult> GetOccurrences(Guid careGroupId, [FromQuery] GetEventOccurrencesRequest request)
        {
            var currentUserId = _currentUserService.UserId;
            var items = await _eventOccurrenceService.GetOccurrencesAsync(currentUserId, careGroupId, request.From, request.To);
            var response = new ApiResponse<IEnumerable<EventOccurrenceResponse>>(items, "查詢事件實例成功", HttpContext.TraceIdentifier);
            return Ok(response);
        }

        /// <summary>
        /// 取消單次事件實例。
        /// </summary>
        /// <param name="careGroupId">照護群組 ID。</param>
        /// <param name="request">要取消的單次事件資料。請帶 eventSeriesId 與 scheduledStartAt；scheduledStartAt 應直接使用事件實例查詢 API 回傳的時間。</param>
        [HttpPost("cancel")]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [EndpointSummary("取消單次事件實例")]
        [EndpointDescription("取消事件系列中的單次事件。這個操作只會取消指定那一次，不會影響整個系列，也不會影響其他週。後端會在 event_occurrences 建立或更新一筆 override，將該次事件狀態標記為 Cancelled。")]
        public async Task<IActionResult> CancelOccurrence(Guid careGroupId, [FromBody] CancelEventOccurrenceRequest request)
        {
            var currentUserId = _currentUserService.UserId;
            await _eventOccurrenceService.CancelOccurrenceAsync(currentUserId, careGroupId, request.EventSeriesId, request.ScheduledStartAt);
            var response = new ApiResponse<object>(null, "取消單次事件成功", HttpContext.TraceIdentifier);
            return Ok(response);
        }
    }
}
