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
    /// 事件 Facade API（對應前端 FME-1 ~ FME-6）。
    /// 這層將「事件系列規則」與「單次實例覆寫」兩個底層模組包裝成單一 /events 介面，
    /// 讓前端只處理「事件」這一個主體；內部仍走 event-series / event-occurrences。
    /// 前端串接請優先使用這組 API；只有在需要分頁、單筆系列規則等進階用途時才呼叫 /event-series 或 /event-occurrences。
    /// </summary>
    [Authorize]
    [ApiController]
    [Tags("Events (建議：對應前端 FME-1 ~ FME-6)")]
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
        [EndpointDescription("建立一個事件。repeatPattern 傳 none 即為單次事件。設計稿未提供的欄位（結束條件、間隔、星期）由後端填入預設值：endType=never、repeatInterval=1、weekly 時自動取 scheduledAt 當天的星期。")]
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
        [EndpointDescription("依 from 與 to 查詢區間內所有事件實例，已套用單次覆寫。回傳額外包含 series 的 repeatPattern，方便前端顯示重複標籤。")]
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
        [EndpointDescription("更新事件系列的規則與預設內容。已被單次覆寫的 occurrence 不受影響。")]
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
        [EndpointDescription("soft delete 整個事件系列；刪除後該系列展開的 occurrence 不再出現在 FME-2 結果。")]
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
        [EndpointSummary("FME-5 編輯重複事件狀態（單次）")]
        [EndpointDescription("針對 {eventId}（系列）中 scheduledAt 所指的單次實例，設定狀態。status 可傳 completed / cancelled / pending；pending 代表清除 override 回復系列預設。")]
        public async Task<IActionResult> UpdateInstanceStatus(Guid careGroupId, Guid eventId, [FromBody] UpdateInstanceStatusRequest request)
        {
            var userId = _currentUserService.UserId;
            await _eventFacade.UpdateInstanceStatusAsync(userId, careGroupId, eventId, request);
            var response = new ApiResponse<object>(null, "更新重複事件狀態成功", HttpContext.TraceIdentifier);
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
        [EndpointDescription("取得 {eventId}（系列）中 scheduledAt 所指單次實例的當前內容與狀態。若前端已透過 FME-2 取得完整列表，此 API 可不必呼叫。")]
        public async Task<IActionResult> GetInstanceStatus(Guid careGroupId, Guid eventId, [FromQuery] DateTime scheduledAt)
        {
            var userId = _currentUserService.UserId;
            var item = await _eventFacade.GetInstanceStatusAsync(userId, careGroupId, eventId, scheduledAt);
            var response = new ApiResponse<EventOccurrenceItem>(item, "取得重複事件狀態成功", HttpContext.TraceIdentifier);
            return Ok(response);
        }
    }
}
