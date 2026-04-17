using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using SeasonsCare.Api.DTOs.Common;
using SeasonsCare.Api.DTOs.Events;
using SeasonsCare.Api.Services;

namespace SeasonsCare.Api.Controllers
{
    /// <summary>
    /// 事件 API（FME-1 ~ FME-6）。
    /// </summary>
    [Authorize]
    [ApiController]
    [Tags("Events")]
    [Route("api/care-groups/{careGroupId}/events")]
    public class EventsController : ControllerBase
    {
        private readonly IEventFacadeService _eventFacade;
        private readonly ICurrentUserService _currentUserService;

        public EventsController(IEventFacadeService eventFacade, ICurrentUserService currentUserService)
        {
            _eventFacade = eventFacade;
            _currentUserService = currentUserService;
        }

        // FME-1
        [HttpPost]
        [ProducesResponseType(typeof(ApiResponse<EventResponse>), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [EndpointSummary("FME-1 新增重複事件")]
        [EndpointDescription("建立事件；repeatPattern = none 代表單次事件。")]
        public async Task<IActionResult> CreateEvent(Guid careGroupId, [FromBody] CreateEventRequest request)
        {
            var userId = _currentUserService.UserId;
            var result = await _eventFacade.CreateEventAsync(userId, careGroupId, request);
            var response = new ApiResponse<EventResponse>(result, "建立重複事件成功", HttpContext.TraceIdentifier);
            return StatusCode(201, response);
        }

        // FME-2
        [HttpGet]
        [ProducesResponseType(typeof(ApiResponse<IEnumerable<EventOccurrenceItem>>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [EndpointSummary("FME-2 取得重複事件（依區間展開）")]
        [EndpointDescription("依 from / to 回傳區間內所有事件實例，已套用單次覆寫。")]
        public async Task<IActionResult> GetEvents(Guid careGroupId, [FromQuery] GetEventsRequest request)
        {
            var userId = _currentUserService.UserId;
            var items = await _eventFacade.GetEventsAsync(userId, careGroupId, request);
            var response = new ApiResponse<IEnumerable<EventOccurrenceItem>>(items, "取得重複事件成功", HttpContext.TraceIdentifier);
            return Ok(response);
        }

        // FME-3
        [HttpPost("{eventId}")]
        [ProducesResponseType(typeof(ApiResponse<EventResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [EndpointSummary("FME-3 編輯重複事件")]
        [EndpointDescription("更新整個事件系列；已被單次覆寫的 occurrence 不受影響。")]
        public async Task<IActionResult> UpdateEvent(Guid careGroupId, Guid eventId, [FromBody] UpdateEventRequest request)
        {
            var userId = _currentUserService.UserId;
            var result = await _eventFacade.UpdateEventAsync(userId, careGroupId, eventId, request);
            var response = new ApiResponse<EventResponse>(result, "更新重複事件成功", HttpContext.TraceIdentifier);
            return Ok(response);
        }

        // FME-4
        [HttpDelete("{eventId}")]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [EndpointSummary("FME-4 刪除重複事件")]
        [EndpointDescription("Soft delete 整個事件系列。")]
        public async Task<IActionResult> DeleteEvent(Guid careGroupId, Guid eventId)
        {
            var userId = _currentUserService.UserId;
            await _eventFacade.DeleteEventAsync(userId, careGroupId, eventId);
            var response = new ApiResponse<object>(null, "刪除重複事件成功", HttpContext.TraceIdentifier);
            return Ok(response);
        }

        // FME-5
        [HttpPost("{eventId}/status")]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [EndpointSummary("FME-5 編輯重複事件（單次）")]
        [EndpointDescription("編輯 scheduledAt 指定的單次實例；可傳 status / description / participants。status 僅 pending 代表清除 override。")]
        public async Task<IActionResult> UpdateInstance(Guid careGroupId, Guid eventId, [FromBody] UpdateInstanceRequest request)
        {
            var userId = _currentUserService.UserId;
            await _eventFacade.UpdateInstanceAsync(userId, careGroupId, eventId, request);
            var response = new ApiResponse<object>(null, "更新重複事件單次內容成功", HttpContext.TraceIdentifier);
            return Ok(response);
        }

        // FME-6
        [HttpGet("{eventId}/status")]
        [ProducesResponseType(typeof(ApiResponse<EventOccurrenceItem>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [EndpointSummary("FME-6 取得重複事件狀態（單次）")]
        [EndpointDescription("取得 scheduledAt 指定單次實例的當前內容與狀態。")]
        public async Task<IActionResult> GetInstanceStatus(Guid careGroupId, Guid eventId, [FromQuery] DateTimeOffset scheduledAt)
        {
            var userId = _currentUserService.UserId;
            var item = await _eventFacade.GetInstanceStatusAsync(userId, careGroupId, eventId, scheduledAt);
            var response = new ApiResponse<EventOccurrenceItem>(item, "取得重複事件狀態成功", HttpContext.TraceIdentifier);
            return Ok(response);
        }
    }
}
