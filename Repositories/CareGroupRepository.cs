using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using SeasonsCare.Api.Data;
using SeasonsCare.Api.Models.Entities;

namespace SeasonsCare.Api.Repositories
{
    // [架構導覽] 資料存取層 (Data Access Layer) - Repository
    // 職責：隔離對底層資料庫的直接依賴。封裝 O/RM (Entity Framework Core) 語法，與資料庫進行實際交涉。不應含有 if/else 的業務權限判斷。
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
            // 單純宣告向 EF Core 的追蹤器 (Tracker) 註冊這筆新增，但不馬上異動資料庫實體
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
            // 將上述所有追蹤到的變更 (工作單元)，一次性送交指令給資料庫
            await _context.SaveChangesAsync();
        }
    }
}
