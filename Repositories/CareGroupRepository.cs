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
                .FirstOrDefaultAsync(cg => cg.Id == id);
        }

        public async Task<(List<CareGroup> Data, int TotalCount)> GetPagedByUserIdAsync(Guid userId, int page, int pageSize, string sort)
        {
            IQueryable<CareGroup> query = _context.CareGroups
                .Where(cg => cg.Members.Any(m => m.UserId == userId))
                .Include(cg => cg.Members);

            if (string.Equals(sort, "createdAt_asc", StringComparison.OrdinalIgnoreCase))
            {
                query = query.OrderBy(g => g.CreatedAt);
            }
            else
            {
                query = query.OrderByDescending(g => g.CreatedAt);
            }

            var totalCount = await query.CountAsync();

            var pagedData = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return (pagedData, totalCount);
        }

        public async Task<List<Guid>> GetAccessibleCareGroupIdsAsync(Guid userId)
        {
            return await _context.CareGroups
                .Where(cg => cg.Members.Any(m => m.UserId == userId))
                .OrderByDescending(cg => cg.CreatedAt)
                .Select(cg => cg.Id)
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
                .AnyAsync(m => m.CareGroupId == careGroupId && m.UserId == userId);
        }

        public async Task<CareGroupMember?> GetMemberAsync(Guid careGroupId, Guid userId)
        {
            return await _context.CareGroupMembers
                .FirstOrDefaultAsync(m => m.CareGroupId == careGroupId && m.UserId == userId);
        }

        public async Task<CareGroupMember?> GetMemberIncludingDeletedAsync(Guid careGroupId, Guid userId)
        {
            return await _context.CareGroupMembers
                .IgnoreQueryFilters()
                .FirstOrDefaultAsync(m => m.CareGroupId == careGroupId && m.UserId == userId);
        }

        public async Task SaveChangesAsync()
        {
            await _context.SaveChangesAsync();
        }
    }
}
