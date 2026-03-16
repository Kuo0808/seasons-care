using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using SeasonsCare.Api.Data;
using SeasonsCare.Api.Models.Entities;

namespace SeasonsCare.Api.Repositories
{
    public class CareGroupRepository : ICareGroupRepository
    {
        private readonly ApplicationDbContext _context;

        public CareGroupRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<CareGroup?> GetByIdAsync(Guid id)
        {
            return await _context.CareGroups
                .Include(cg => cg.Members)
                    .ThenInclude(m => m.User)
                .Where(cg => cg.DeletedAt == null)
                .FirstOrDefaultAsync(cg => cg.Id == id);
        }

        public async Task<List<CareGroup>> GetByUserIdAsync(Guid userId)
        {
            return await _context.CareGroups
                .Where(cg => cg.DeletedAt == null && cg.Members.Any(m => m.UserId == userId && m.DeletedAt == null))
                .Include(cg => cg.Members)
                .ToListAsync();
        }

        public async Task AddAsync(CareGroup careGroup)
        {
            await _context.CareGroups.AddAsync(careGroup);
        }

        public async Task AddMemberAsync(CareGroupMember member)
        {
            await _context.CareGroupMembers.AddAsync(member);
        }

        public async Task<bool> IsMemberAsync(Guid careGroupId, Guid userId)
        {
            return await _context.CareGroupMembers
                .AnyAsync(m => m.CareGroupId == careGroupId && m.UserId == userId && m.DeletedAt == null);
        }

        public async Task<CareGroupMember?> GetMemberAsync(Guid careGroupId, Guid userId)
        {
            return await _context.CareGroupMembers
                .FirstOrDefaultAsync(m => m.CareGroupId == careGroupId && m.UserId == userId && m.DeletedAt == null);
        }

        public async Task SaveChangesAsync()
        {
            await _context.SaveChangesAsync();
        }
    }
}
