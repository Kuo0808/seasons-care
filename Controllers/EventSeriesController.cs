using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using SeasonsCare.Api.DTOs.Common;
using SeasonsCare.Api.DTOs.EventSeries;
using SeasonsCare.Api.Services;

namespace SeasonsCare.Api.Controllers
{
    /// <summary>
    /// 事件系列 API。
    /// 這個模組用來建立與管理可重複的系列事件，例如每週固定回診、固定復健、固定陪診。
    /// </summary>
    [Authorize]
    [ApiController]
    [Route("api/care-groups/{careGroupId}/event-series")]
    public class EventSeriesController : ControllerBase
    {
        private readonly IEventSeriesService _eventSeriesService;
        private readonly ICurrentUserService _currentUserService;

        public EventSeriesController(IEventSeriesService eventSeriesService, ICurrentUserService currentUserService)
        {
            _eventSeriesService = eventSeriesService;
            _currentUserService = currentUserService;
        }

        /// <summary>
        /// 取得指定照護群組的事件系列列表。
        /// </summary>
        [HttpGet]
        [ProducesResponseType(typeof(ApiResponse<IEnumerable<EventSeriesResponse>>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [EndpointSummary("取得事件系列列表")]
        [EndpointDescription("取得指定照護群組底下的事件系列列表。前端需在 path 帶入 careGroupId，可另外用 query string 傳入 page、pageSize、sort。此 API 回傳的是系列規則本身，不是展開後的每次事件實例。")]
        public async Task<IActionResult> GetSeries(Guid careGroupId, [FromQuery] PaginationRequest paginationRequest)
        {
            var currentUserId = _currentUserService.UserId;
            var pagedResult = await _eventSeriesService.GetSeriesAsync(currentUserId, careGroupId, paginationRequest);
            var response = new ApiResponse<IEnumerable<EventSeriesResponse>>(
                pagedResult.Items,
                "取得事件系列列表成功",
                HttpContext.TraceIdentifier,
                pagedResult.Pagination);
            return Ok(response);
        }

        /// <summary>
        /// 取得單筆事件系列。
        /// </summary>
        [HttpGet("{seriesId}")]
        [ProducesResponseType(typeof(ApiResponse<EventSeriesResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [EndpointSummary("取得單筆事件系列")]
        [EndpointDescription("依照 path 參數 careGroupId 與 seriesId 取得單筆事件系列。若該系列不屬於目前照護群組，會回傳 404。")]
        public async Task<IActionResult> GetSeriesById(Guid careGroupId, Guid seriesId)
        {
            var currentUserId = _currentUserService.UserId;
            var result = await _eventSeriesService.GetSeriesByIdAsync(currentUserId, careGroupId, seriesId);
            var response = new ApiResponse<EventSeriesResponse>(result, "取得事件系列成功", HttpContext.TraceIdentifier);
            return Ok(response);
        }

        /// <summary>
        /// 建立事件系列。
        /// </summary>
        [HttpPost]
        [ProducesResponseType(typeof(ApiResponse<EventSeriesResponse>), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [EndpointSummary("建立事件系列")]
        [EndpointDescription("在指定照護群組底下建立新的事件系列。前端需在 request body 提供 title、startsAt 與重複規則欄位；participants 若有值，必須全部是該群組成員的 userId。這一版先建立系列規則，不會在此 API 直接回傳展開後的事件實例。")]
        public async Task<IActionResult> CreateSeries(Guid careGroupId, [FromBody] CreateEventSeriesRequest request)
        {
            var currentUserId = _currentUserService.UserId;
            var result = await _eventSeriesService.CreateSeriesAsync(currentUserId, careGroupId, request);
            var response = new ApiResponse<EventSeriesResponse>(result, "建立事件系列成功", HttpContext.TraceIdentifier);
            return StatusCode(201, response);
        }
    }
}
