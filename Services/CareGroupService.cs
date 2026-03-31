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
                Name = request.RecipientName,
                RecipientName = request.RecipientName,
                RecipientGender = request.RecipientGender,
                RecipientBirthDate = request.RecipientBirthDate,
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
                RecipientGender = careGroup.RecipientGender,
                RecipientBirthDate = careGroup.RecipientBirthDate,
                Description = careGroup.Description,
                HealthStatus = careGroup.HealthStatus,
                InviteCode = careGroup.InviteCode,
                CreatedAt = careGroup.CreatedAt,
                MemberCount = 1
            };
        }

        public async Task<PagedResponse<CareGroupResponse>> GetMyGroupsAsync(Guid currentUserId, PaginationRequest pagination)
        {
            var (pagedData, totalCount) = await _careGroupRepository.GetPagedByUserIdAsync(currentUserId, pagination.Page, pagination.PageSize, pagination.Sort);

            var pagedGroups = pagedData.Select(g => new CareGroupResponse
            {
                Id = g.Id,
                Name = g.Name,
                RecipientName = g.RecipientName,
                RecipientGender = g.RecipientGender,
                RecipientBirthDate = g.RecipientBirthDate,
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
            var group = await _careGroupRepository.GetByIdAsync(careGroupId);
            if (group == null)
            {
                throw new DomainException("Care group not found.", "NOT_FOUND", 404);
            }

            var isMember = await _careGroupRepository.IsMemberAsync(careGroupId, currentUserId);
            if (!isMember)
            {
                throw new DomainException("You are not a member of this care group.", "FORBIDDEN_ACCESS", 403);
            }

            return new CareGroupDetailResponse
            {
                Id = group.Id,
                Name = group.Name,
                RecipientName = group.RecipientName,
                RecipientGender = group.RecipientGender,
                RecipientBirthDate = group.RecipientBirthDate,
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
            var group = await _careGroupRepository.GetByIdAsync(careGroupId);
            if (group == null)
            {
                throw new DomainException("Care group not found.", "NOT_FOUND", 404);
            }

            var isMember = await _careGroupRepository.IsMemberAsync(careGroupId, currentUserId);
            if (!isMember)
            {
                throw new DomainException("You are not a member of this care group.", "FORBIDDEN_ACCESS", 403);
            }

            group.Name = request.Name;
            group.RecipientName = request.RecipientName;
            group.RecipientGender = request.RecipientGender;
            group.RecipientBirthDate = request.RecipientBirthDate;
            group.Description = request.Description;
            group.HealthStatus = request.HealthStatus;
            group.UpdatedAt = DateTime.UtcNow;

            await _careGroupRepository.SaveChangesAsync();

            return new CareGroupResponse
            {
                Id = group.Id,
                Name = group.Name,
                RecipientName = group.RecipientName,
                RecipientGender = group.RecipientGender,
                RecipientBirthDate = group.RecipientBirthDate,
                Description = group.Description,
                HealthStatus = group.HealthStatus,
                InviteCode = group.InviteCode,
                CreatedAt = group.CreatedAt,
                MemberCount = group.Members.Count(m => m.DeletedAt == null)
            };
        }

        public async Task JoinAsync(Guid currentUserId, Guid careGroupId, JoinCareGroupRequest request)
        {
            var group = await _careGroupRepository.GetByIdAsync(careGroupId);
            if (group == null)
            {
                throw new DomainException("Care group not found.", "NOT_FOUND", 404);
            }

            var existingMember = await _careGroupRepository.GetMemberIncludingDeletedAsync(careGroupId, currentUserId);
            if (existingMember != null && existingMember.DeletedAt == null)
            {
                throw new DomainException("You are already a member of this care group.", "CONFLICT", 409);
            }

            if (!string.IsNullOrEmpty(group.InviteCode))
            {
                if (string.IsNullOrEmpty(request.InviteCode) || !string.Equals(group.InviteCode, request.InviteCode, StringComparison.OrdinalIgnoreCase))
                {
                    throw new DomainException("Invite code is invalid.", "UNAUTHORIZED_JOIN", 401);
                }
            }

            if (existingMember != null)
            {
                existingMember.Role = CareGroupRole.Member;
                existingMember.JoinedAt = DateTime.UtcNow;
                existingMember.DeletedAt = null;
                existingMember.UpdatedAt = DateTime.UtcNow;
                await _careGroupRepository.SaveChangesAsync();
                return;
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

            if (currentUserId != memberUserId && currentUserMember.Role != CareGroupRole.Admin)
            {
                throw new DomainException("Only admins can remove other members.", "FORBIDDEN_ACCESS", 403);
            }

            var memberToRemove = await _careGroupRepository.GetMemberAsync(careGroupId, memberUserId);
            if (memberToRemove == null)
            {
                throw new DomainException("Member not found.", "NOT_FOUND", 404);
            }

            memberToRemove.DeletedAt = DateTime.UtcNow;
            await _careGroupRepository.SaveChangesAsync();
        }
    }
}
