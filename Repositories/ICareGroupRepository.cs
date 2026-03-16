using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using SeasonsCare.Api.Models.Entities;

namespace SeasonsCare.Api.Repositories
{
    public interface ICareGroupRepository
    {
        Task<CareGroup?> GetByIdAsync(Guid id);
        Task<List<CareGroup>> GetByUserIdAsync(Guid userId);
        Task AddAsync(CareGroup careGroup);
        Task AddMemberAsync(CareGroupMember member);
        Task<bool> IsMemberAsync(Guid careGroupId, Guid userId);
        Task<CareGroupMember?> GetMemberAsync(Guid careGroupId, Guid userId);
        Task SaveChangesAsync();
    }
}
