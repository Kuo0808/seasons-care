using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using SeasonsCare.Api.Config;
using SeasonsCare.Api.DTOs.CareGroups;
using SeasonsCare.Api.DTOs.Common;
using SeasonsCare.Api.Exceptions;
using SeasonsCare.Api.Models.Entities;
using SeasonsCare.Api.Models.Enums;
using SeasonsCare.Api.Repositories;

namespace SeasonsCare.Api.Services
{
    // [架構導覽] 商業邏輯層 (Business Logic Layer) - Service
    // 職責：系統的核心大腦。負責執行特定領域規則 (Domain Rules)、跨實體操作、權限查核與資料映射。確保每次呼叫 Repository 都是合法且正確的狀態。
    public class CareGroupService : ICareGroupService
    {
        private readonly ICareGroupRepository _careGroupRepository;

        public CareGroupService(ICareGroupRepository careGroupRepository)
        {
            _careGroupRepository = careGroupRepository;
        }

        public async Task<CareGroupResponse> CreateAsync(Guid currentUserId, CreateCareGroupRequest request)
        {
            // 步驟 1：依據傳入的 DTO 初始化核心的領域實體 (Entity)，並設定基礎屬性 (自動產生邀請碼)
            var careGroup = new CareGroup
            {
                Name = request.RecipientName,
                RecipientName = request.RecipientName,
                RecipientGender = request.RecipientGender,
                RecipientBirthDate = request.RecipientBirthDate,
                InviteCode = Guid.NewGuid().ToString("N").Substring(0, 8).ToUpper(),
                CreatedBy = currentUserId.ToString()
            };

            // 步驟 2：利用單一工作單元 (Unit of Work) 宣告新增群組行為
            await _careGroupRepository.AddAsync(careGroup);

            // 步驟 3：補充建立關聯實體 (將建立者本人預設為群組管理員)
            var member = new CareGroupMember
            {
                CareGroupId = careGroup.Id,
                UserId = currentUserId,
                Role = CareGroupRole.Admin,
                CreatedBy = currentUserId.ToString()
            };

            await _careGroupRepository.AddMemberAsync(member);
            
            // 步驟 4：統一觸發 SaveChanges，確保群組與管理員身分被綁在同一個交易任務 (Transaction) 中寫入資料庫
            await _careGroupRepository.SaveChangesAsync();

            // 步驟 5：將結果封裝成 Response DTO 再往前端吐回，不對展示層曝露實際資料庫實體本身
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
                        AvatarKey = m.User.AvatarKey,
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
            group.UpdatedAt = TimeHelper.Now;

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

        public async Task JoinByInviteCodeAsync(Guid currentUserId, JoinCareGroupRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.InviteCode))
            {
                throw new DomainException("Invite code is required.", "VALIDATION_FAILED", 400);
            }

            var normalizedInviteCode = request.InviteCode.Trim();
            var group = await _careGroupRepository.GetByInviteCodeAsync(normalizedInviteCode);
            if (group == null)
            {
                throw new DomainException("Invite code is invalid.", "UNAUTHORIZED_JOIN", 401);
            }

            await JoinInternalAsync(currentUserId, group, normalizedInviteCode);
        }

        public async Task JoinAsync(Guid currentUserId, Guid careGroupId, JoinCareGroupRequest request)
        {
            var group = await _careGroupRepository.GetByIdAsync(careGroupId);
            if (group == null)
            {
                throw new DomainException("Care group not found.", "NOT_FOUND", 404);
            }

            await JoinInternalAsync(currentUserId, group, request.InviteCode);
        }

        private async Task JoinInternalAsync(Guid currentUserId, CareGroup group, string? inviteCode)
        {
            var careGroupId = group.Id;
            var existingMember = await _careGroupRepository.GetMemberIncludingDeletedAsync(careGroupId, currentUserId);
            if (existingMember != null && existingMember.DeletedAt == null)
            {
                throw new DomainException("You are already a member of this care group.", "CONFLICT", 409);
            }

            if (!string.IsNullOrEmpty(group.InviteCode))
            {
                if (string.IsNullOrEmpty(inviteCode) || !string.Equals(group.InviteCode, inviteCode, StringComparison.OrdinalIgnoreCase))
                {
                    throw new DomainException("Invite code is invalid.", "UNAUTHORIZED_JOIN", 401);
                }
            }

            if (existingMember != null)
            {
                existingMember.Role = CareGroupRole.Member;
                existingMember.JoinedAt = TimeHelper.Now;
                existingMember.DeletedAt = null;
                existingMember.UpdatedAt = TimeHelper.Now;
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

            memberToRemove.DeletedAt = TimeHelper.Now;
            await _careGroupRepository.SaveChangesAsync();
        }
    }
}
