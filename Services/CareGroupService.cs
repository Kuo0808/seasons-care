using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using SeasonsCare.Api.DTOs.CareGroups;
using SeasonsCare.Api.DTOs.Common;
using SeasonsCare.Api.Exceptions;
using SeasonsCare.Api.Models.Entities;
using SeasonsCare.Api.Models.Enums;
using SeasonsCare.Api.Repositories;

namespace SeasonsCare.Api.Services
{
    public class CareGroupService : ICareGroupService
    {
        private readonly ICareGroupRepository _careGroupRepository;

        public CareGroupService(ICareGroupRepository careGroupRepository)
        {
            _careGroupRepository = careGroupRepository;
        }

        public async Task<CareGroupResponse> CreateAsync(Guid currentUserId, CreateCareGroupRequest request)
        {
            var careGroup = new CareGroup
            {
                Name = request.Name,
                RecipientName = request.RecipientName,
                Description = request.Description,
                HealthStatus = request.HealthStatus,
                InviteCode = Guid.NewGuid().ToString("N").Substring(0, 8).ToUpper(),
                CreatedBy = currentUserId.ToString()
            };

            await _careGroupRepository.AddAsync(careGroup);

            var member = new CareGroupMember
            {
                CareGroupId = careGroup.Id,
                UserId = currentUserId,
                Role = CareGroupRole.Admin,
                CreatedBy = currentUserId.ToString()
            };

            await _careGroupRepository.AddMemberAsync(member);
            await _careGroupRepository.SaveChangesAsync();

            return new CareGroupResponse
            {
                Id = careGroup.Id,
                Name = careGroup.Name,
                RecipientName = careGroup.RecipientName,
                Description = careGroup.Description,
                HealthStatus = careGroup.HealthStatus,
                InviteCode = careGroup.InviteCode,
                CreatedAt = careGroup.CreatedAt,
                MemberCount = 1
            };
        }

        public async Task<PagedResponse<CareGroupResponse>> GetMyGroupsAsync(Guid currentUserId, PaginationRequest pagination)
        {
            var groups = await _careGroupRepository.GetByUserIdAsync(currentUserId);
            var query = groups.AsQueryable();

            if (pagination.Sort.Equals("createdAt_desc", StringComparison.OrdinalIgnoreCase))
            {
                query = query.OrderByDescending(g => g.CreatedAt);
            }
            else
            {
                query = query.OrderByDescending(g => g.CreatedAt);
            }

            var totalCount = query.Count();

            var pagedGroups = query
                .Skip((pagination.Page - 1) * pagination.PageSize)
                .Take(pagination.PageSize)
                .Select(g => new CareGroupResponse
                {
                    Id = g.Id,
                    Name = g.Name,
                    RecipientName = g.RecipientName,
                    Description = g.Description,
                    HealthStatus = g.HealthStatus,
                    InviteCode = g.InviteCode,
                    CreatedAt = g.CreatedAt,
                    MemberCount = g.Members.Count(m => m.DeletedAt == null)
                }).ToList();

            return new PagedResponse<CareGroupResponse>(pagedGroups, totalCount, pagination.Page, pagination.PageSize);
        }

        public async Task<CareGroupDetailResponse> GetByIdAsync(Guid currentUserId, Guid careGroupId)
        {
            var isMember = await _careGroupRepository.IsMemberAsync(careGroupId, currentUserId);
            if (!isMember)
            {
                throw new DomainException("You are not a member of this care group.", "FORBIDDEN_ACCESS", 403);
            }

            var group = await _careGroupRepository.GetByIdAsync(careGroupId);
            if (group == null)
            {
                throw new DomainException("Care group not found.", "NOT_FOUND", 404);
            }

            return new CareGroupDetailResponse
            {
                Id = group.Id,
                Name = group.Name,
                RecipientName = group.RecipientName,
                Description = group.Description,
                HealthStatus = group.HealthStatus,
                InviteCode = group.InviteCode,
                CreatedAt = group.CreatedAt,
                Members = group.Members
                    .Where(m => m.DeletedAt == null && m.User != null)
                    .Select(m => new CareGroupMemberResponse
                    {
                        UserId = m.UserId,
                        Username = m.User.Username,
                        Role = m.Role,
                        JoinedAt = m.JoinedAt
                    }).ToList()
            };
        }

        public async Task<CareGroupResponse> UpdateAsync(Guid currentUserId, Guid careGroupId, UpdateCareGroupRequest request)
        {
            var isMember = await _careGroupRepository.IsMemberAsync(careGroupId, currentUserId);
            if (!isMember)
            {
                throw new DomainException("You are not a member of this care group.", "FORBIDDEN_ACCESS", 403);
            }

            var group = await _careGroupRepository.GetByIdAsync(careGroupId);
            if (group == null)
            {
                throw new DomainException("Care group not found.", "NOT_FOUND", 404);
            }

            group.Name = request.Name;
            group.RecipientName = request.RecipientName;
            group.Description = request.Description;
            group.HealthStatus = request.HealthStatus;
            group.UpdatedAt = DateTime.UtcNow;

            await _careGroupRepository.SaveChangesAsync();

            return new CareGroupResponse
            {
                Id = group.Id,
                Name = group.Name,
                RecipientName = group.RecipientName,
                Description = group.Description,
                HealthStatus = group.HealthStatus,
                InviteCode = group.InviteCode,
                CreatedAt = group.CreatedAt,
                MemberCount = group.Members.Count(m => m.DeletedAt == null)
            };
        }

        public async Task JoinAsync(Guid currentUserId, Guid careGroupId, JoinCareGroupRequest request)
        {
            var isMember = await _careGroupRepository.IsMemberAsync(careGroupId, currentUserId);
            if (isMember)
            {
                throw new DomainException("You are already a member of this care group.", "CONFLICT", 409);
            }

            var group = await _careGroupRepository.GetByIdAsync(careGroupId);
            if (group == null)
            {
                throw new DomainException("Care group not found.", "NOT_FOUND", 404);
            }

            if (!string.IsNullOrEmpty(group.InviteCode))
            {
                if (string.IsNullOrEmpty(request.InviteCode) || !string.Equals(group.InviteCode, request.InviteCode, StringComparison.OrdinalIgnoreCase))
                {
                    throw new DomainException("Invalid invite code.", "UNAUTHORIZED_JOIN", 401);
                }
            }

            var newMember = new CareGroupMember
            {
                CareGroupId = careGroupId,
                UserId = currentUserId,
                Role = CareGroupRole.Member,
                CreatedBy = currentUserId.ToString()
            };

            await _careGroupRepository.AddMemberAsync(newMember);
            await _careGroupRepository.SaveChangesAsync();
        }

        public async Task RemoveMemberAsync(Guid currentUserId, Guid careGroupId, Guid memberUserId)
        {
            var group = await _careGroupRepository.GetByIdAsync(careGroupId);
            if (group == null)
            {
                throw new DomainException("Care group not found.", "NOT_FOUND", 404);
            }

            var currentUserMember = await _careGroupRepository.GetMemberAsync(careGroupId, currentUserId);
            if (currentUserMember == null)
            {
                throw new DomainException("You are not a member of this care group.", "FORBIDDEN_ACCESS", 403);
            }

            if (currentUserId != memberUserId)
            {
                if (currentUserMember.Role != CareGroupRole.Admin)
                {
                    throw new DomainException("Only admins can remove other members.", "FORBIDDEN_ACCESS", 403);
                }
            }

            var memberToRemove = await _careGroupRepository.GetMemberAsync(careGroupId, memberUserId);
            if (memberToRemove == null)
            {
                throw new DomainException("Member not found in this group.", "NOT_FOUND", 404);
            }

            memberToRemove.DeletedAt = DateTime.UtcNow;
            await _careGroupRepository.SaveChangesAsync();
        }
    }
}
