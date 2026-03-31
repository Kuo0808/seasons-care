using SeasonsCare.Api.DTOs.CareGroups;
using SeasonsCare.Api.Exceptions;
using SeasonsCare.Api.Models.Entities;
using SeasonsCare.Api.Models.Enums;
using SeasonsCare.Api.Repositories;
using SeasonsCare.Api.Services;

namespace SeasonsCare.Api.Tests.Services;

public class CareGroupServiceTests
{
    [Fact]
    public async Task CreateAsync_CreatesGroup_AndAddsCreatorAsAdmin()
    {
        var repository = new FakeCareGroupRepository();
        var service = new CareGroupService(repository);
        var userId = Guid.NewGuid();

        var result = await service.CreateAsync(userId, new CreateCareGroupRequest
        {
            RecipientName = "Dad",
            RecipientGender = "Male",
            RecipientBirthDate = new DateOnly(1950, 1, 2)
        });

        Assert.Equal("Dad", result.Name);
        Assert.Equal("Male", result.RecipientGender);
        Assert.Equal(new DateOnly(1950, 1, 2), result.RecipientBirthDate);
        Assert.Equal(1, result.MemberCount);
        Assert.Single(repository.Groups);
        Assert.Single(repository.Members);
        Assert.Equal(userId, repository.Members[0].UserId);
        Assert.Equal(CareGroupRole.Admin, repository.Members[0].Role);
        Assert.Equal(repository.Groups[0].Id, repository.Members[0].CareGroupId);
    }

    [Fact]
    public async Task GetByIdAsync_ThrowsForbidden_WhenUserIsNotMember()
    {
        var group = new CareGroup { Id = Guid.NewGuid(), Name = "Home Care", RecipientName = "Dad", RecipientGender = "Male", RecipientBirthDate = new DateOnly(1950, 1, 2) };
        var repository = new FakeCareGroupRepository(group);
        repository.IsMemberResult = false;
        var service = new CareGroupService(repository);

        var exception = await Assert.ThrowsAsync<DomainException>(() =>
            service.GetByIdAsync(Guid.NewGuid(), group.Id));

        Assert.Equal(403, exception.StatusCode);
        Assert.Equal("FORBIDDEN_ACCESS", exception.ErrorCode);
    }

    [Fact]
    public async Task GetByIdAsync_ThrowsNotFound_WhenGroupDoesNotExist()
    {
        var repository = new FakeCareGroupRepository();
        var service = new CareGroupService(repository);

        var exception = await Assert.ThrowsAsync<DomainException>(() =>
            service.GetByIdAsync(Guid.NewGuid(), Guid.NewGuid()));

        Assert.Equal(404, exception.StatusCode);
        Assert.Equal("NOT_FOUND", exception.ErrorCode);
    }

    [Fact]
    public async Task JoinAsync_ThrowsConflict_WhenUserAlreadyJoined()
    {
        var userId = Guid.NewGuid();
        var group = new CareGroup { Id = Guid.NewGuid(), Name = "Home Care", RecipientName = "Dad", RecipientGender = "Male", RecipientBirthDate = new DateOnly(1950, 1, 2), InviteCode = "JOIN1234" };
        var activeMember = new CareGroupMember
        {
            CareGroupId = group.Id,
            UserId = userId,
            Role = CareGroupRole.Member
        };

        var repository = new FakeCareGroupRepository(group);
        repository.MemberLookup[(group.Id, userId)] = activeMember;
        var service = new CareGroupService(repository);

        var exception = await Assert.ThrowsAsync<DomainException>(() =>
            service.JoinAsync(userId, group.Id, new JoinCareGroupRequest { InviteCode = "JOIN1234" }));

        Assert.Equal(409, exception.StatusCode);
        Assert.Equal("CONFLICT", exception.ErrorCode);
    }

    [Fact]
    public async Task JoinAsync_RestoresSoftDeletedMembership_WhenUserRejoins()
    {
        var userId = Guid.NewGuid();
        var group = new CareGroup { Id = Guid.NewGuid(), Name = "Home Care", RecipientName = "Dad", RecipientGender = "Male", RecipientBirthDate = new DateOnly(1950, 1, 2), InviteCode = "JOIN1234" };
        var deletedMember = new CareGroupMember
        {
            CareGroupId = group.Id,
            UserId = userId,
            Role = CareGroupRole.Member,
            DeletedAt = DateTime.UtcNow.AddDays(-1)
        };

        var repository = new FakeCareGroupRepository(group);
        repository.MemberLookup[(group.Id, userId)] = deletedMember;
        var service = new CareGroupService(repository);

        await service.JoinAsync(userId, group.Id, new JoinCareGroupRequest { InviteCode = "JOIN1234" });

        Assert.Empty(repository.Members);
        Assert.Null(deletedMember.DeletedAt);
        Assert.NotNull(deletedMember.UpdatedAt);
        Assert.Equal(CareGroupRole.Member, deletedMember.Role);
    }

    [Fact]
    public async Task RemoveMemberAsync_ThrowsForbidden_WhenNonAdminRemovesAnotherMember()
    {
        var careGroupId = Guid.NewGuid();
        var currentUserId = Guid.NewGuid();
        var memberToRemoveId = Guid.NewGuid();
        var group = new CareGroup { Id = careGroupId, Name = "Home Care", RecipientName = "Dad", RecipientGender = "Male", RecipientBirthDate = new DateOnly(1950, 1, 2) };
        var repository = new FakeCareGroupRepository(group);
        repository.MemberLookup[(careGroupId, currentUserId)] = new CareGroupMember
        {
            CareGroupId = careGroupId,
            UserId = currentUserId,
            Role = CareGroupRole.Member
        };
        repository.MemberLookup[(careGroupId, memberToRemoveId)] = new CareGroupMember
        {
            CareGroupId = careGroupId,
            UserId = memberToRemoveId,
            Role = CareGroupRole.Member
        };

        var service = new CareGroupService(repository);

        var exception = await Assert.ThrowsAsync<DomainException>(() =>
            service.RemoveMemberAsync(currentUserId, careGroupId, memberToRemoveId));

        Assert.Equal(403, exception.StatusCode);
        Assert.Equal("FORBIDDEN_ACCESS", exception.ErrorCode);
    }

    [Fact]
    public async Task RemoveMemberAsync_SoftDeletesTargetMember_WhenAdminRemovesMember()
    {
        var careGroupId = Guid.NewGuid();
        var currentUserId = Guid.NewGuid();
        var memberToRemoveId = Guid.NewGuid();
        var group = new CareGroup { Id = careGroupId, Name = "Home Care", RecipientName = "Dad", RecipientGender = "Male", RecipientBirthDate = new DateOnly(1950, 1, 2) };
        var targetMember = new CareGroupMember
        {
            CareGroupId = careGroupId,
            UserId = memberToRemoveId,
            Role = CareGroupRole.Member
        };

        var repository = new FakeCareGroupRepository(group);
        repository.MemberLookup[(careGroupId, currentUserId)] = new CareGroupMember
        {
            CareGroupId = careGroupId,
            UserId = currentUserId,
            Role = CareGroupRole.Admin
        };
        repository.MemberLookup[(careGroupId, memberToRemoveId)] = targetMember;

        var service = new CareGroupService(repository);

        await service.RemoveMemberAsync(currentUserId, careGroupId, memberToRemoveId);

        Assert.NotNull(targetMember.DeletedAt);
    }

    private sealed class FakeCareGroupRepository : ICareGroupRepository
    {
        public List<CareGroup> Groups { get; } = new();
        public List<CareGroupMember> Members { get; } = new();
        public Dictionary<(Guid CareGroupId, Guid UserId), CareGroupMember> MemberLookup { get; } = new();
        public bool IsMemberResult { get; set; }

        public FakeCareGroupRepository(params CareGroup[] groups)
        {
            Groups.AddRange(groups);
        }

        public Task<CareGroup?> GetByIdAsync(Guid id)
        {
            return Task.FromResult(Groups.FirstOrDefault(x => x.Id == id));
        }

        public Task<(List<CareGroup> Data, int TotalCount)> GetPagedByUserIdAsync(Guid userId, int page, int pageSize, string sort)
        {
            return Task.FromResult((Groups.ToList(), Groups.Count));
        }

        public Task<List<Guid>> GetAccessibleCareGroupIdsAsync(Guid userId)
        {
            return Task.FromResult(Groups.Select(x => x.Id).ToList());
        }

        public Task AddAsync(CareGroup careGroup)
        {
            Groups.Add(careGroup);
            return Task.CompletedTask;
        }

        public Task AddMemberAsync(CareGroupMember member)
        {
            Members.Add(member);
            MemberLookup[(member.CareGroupId, member.UserId)] = member;
            return Task.CompletedTask;
        }

        public Task<bool> IsMemberAsync(Guid careGroupId, Guid userId)
        {
            return Task.FromResult(IsMemberResult || (MemberLookup.TryGetValue((careGroupId, userId), out var member) && member.DeletedAt == null));
        }

        public Task<CareGroupMember?> GetMemberAsync(Guid careGroupId, Guid userId)
        {
            MemberLookup.TryGetValue((careGroupId, userId), out var member);
            if (member?.DeletedAt != null)
            {
                member = null;
            }
            return Task.FromResult(member);
        }

        public Task<CareGroupMember?> GetMemberIncludingDeletedAsync(Guid careGroupId, Guid userId)
        {
            MemberLookup.TryGetValue((careGroupId, userId), out var member);
            return Task.FromResult(member);
        }

        public Task SaveChangesAsync()
        {
            return Task.CompletedTask;
        }
    }
}
