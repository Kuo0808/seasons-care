using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SeasonsCare.Api.DTOs.Common;
using SeasonsCare.Api.DTOs.BloodPressures;
using SeasonsCare.Api.Services;

namespace SeasonsCare.Api.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/care-groups/{careGroupId}/blood-pressures")]
    public class BloodPressuresController : ControllerBase
    {
        private readonly IBloodPressureService _bloodPressureService;
        private readonly ICurrentUserService _currentUserService;

        public BloodPressuresController(IBloodPressureService bloodPressureService, ICurrentUserService currentUserService)
        {
            _bloodPressureService = bloodPressureService;
            _currentUserService = currentUserService;
        }

        [HttpGet]
        public async Task<IActionResult> GetRecords(Guid careGroupId, [FromQuery] PaginationRequest request)
        {
            var currentUserId = _currentUserService.UserId;
            var pagedResult = await _bloodPressureService.GetRecordsAsync(currentUserId, careGroupId, request);
            
            var response = new ApiResponse<IEnumerable<BloodPressureResponse>>(
                pagedResult.Items, 
                "取得血壓紀錄列表成功", 
                HttpContext.TraceIdentifier, 
                pagedResult.Pagination);
            return Ok(response);
        }

        [HttpGet("{recordId}")]
        public async Task<IActionResult> GetRecordById(Guid careGroupId, Guid recordId)
        {
            var currentUserId = _currentUserService.UserId;
            var result = await _bloodPressureService.GetRecordByIdAsync(currentUserId, careGroupId, recordId);
            var response = new ApiResponse<BloodPressureResponse>(result, "取得血壓紀錄成功", HttpContext.TraceIdentifier);
            return Ok(response);
        }

        [HttpPost]
        public async Task<IActionResult> CreateRecord(Guid careGroupId, [FromBody] CreateBloodPressureRequest request)
        {
            var currentUserId = _currentUserService.UserId;
            var result = await _bloodPressureService.CreateRecordAsync(currentUserId, careGroupId, request);
            var response = new ApiResponse<BloodPressureResponse>(result, "建立血壓紀錄成功", HttpContext.TraceIdentifier);
            return StatusCode(201, response);
        }

        [HttpPut("{recordId}")]
        public async Task<IActionResult> UpdateRecord(Guid careGroupId, Guid recordId, [FromBody] UpdateBloodPressureRequest request)
        {
            var currentUserId = _currentUserService.UserId;
            var result = await _bloodPressureService.UpdateRecordAsync(currentUserId, careGroupId, recordId, request);
            var response = new ApiResponse<BloodPressureResponse>(result, "更新血壓紀錄成功", HttpContext.TraceIdentifier);
            return Ok(response);
        }

        [HttpDelete("{recordId}")]
        public async Task<IActionResult> DeleteRecord(Guid careGroupId, Guid recordId)
        {
            var currentUserId = _currentUserService.UserId;
            await _bloodPressureService.DeleteRecordAsync(currentUserId, careGroupId, recordId);
            var response = new ApiResponse<object>(null, "刪除血壓紀錄成功", HttpContext.TraceIdentifier);
            return Ok(response);
        }
    }
}
