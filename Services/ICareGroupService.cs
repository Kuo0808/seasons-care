using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using SeasonsCare.Api.DTOs.CareGroups;

namespace SeasonsCare.Api.Services
{
    public interface ICareGroupService
    {
        Task<CareGroupResponse> CreateAsync(Guid currentUserId, CreateCareGroupRequest request);
        Task<List<CareGroupResponse>> GetMyGroupsAsync(Guid currentUserId);
        Task<CareGroupDetailResponse> GetByIdAsync(Guid currentUserId, Guid careGroupId);
        Task<CareGroupResponse> UpdateAsync(Guid currentUserId, Guid careGroupId, UpdateCareGroupRequest request);
        Task JoinAsync(Guid currentUserId, Guid careGroupId, JoinCareGroupRequest request);
        Task RemoveMemberAsync(Guid currentUserId, Guid careGroupId, Guid memberUserId);
    }
}
