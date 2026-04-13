using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using SeasonsCare.Api.Data;
using SeasonsCare.Api.Models.Entities.HealthRecords;
using SeasonsCare.Api.DTOs.Common;

namespace SeasonsCare.Api.Repositories.HealthRecords
{
    // [架構導覽] 資料存取層 (Data Access Layer) - Repository
    // 職責：隔離對底層資料庫的直接依賴。封裝 O/RM (Entity Framework Core) 語法，與資料庫進行實際交涉。不應含有 if/else 的業務權限判斷。
    public class BloodSugarRepository : IBloodSugarRepository
    {
        private readonly DbContext _context;

        public BloodSugarRepository(DbContext context)
        {
            _context = context;
        }

        public async Task<PagedResponse<BloodSugarRecord>> GetPagedAsync(Guid careGroupId, PaginationRequest request)
        {
            var query = _context.Set<BloodSugarRecord>()
                .Where(x => x.CareGroupId == careGroupId)
                .ApplyDateRange(request, x => x.RecordDate);

            // 健康數據列表統一依 recordDate 作為預設排序欄位。
            query = request.Sort switch
            {
                "recordDate_asc" => query.OrderBy(x => x.RecordDate),
                "recordDate_desc" => query.OrderByDescending(x => x.RecordDate),
                "createdAt_asc" => query.OrderBy(x => x.CreatedAt),
                "createdAt_desc" => query.OrderByDescending(x => x.CreatedAt),
                _ => query.OrderByDescending(x => x.RecordDate)
            };

            var totalCount = await query.CountAsync();
            var items = await query
                .Skip((request.Page - 1) * request.PageSize)
                .Take(request.PageSize)
                .ToListAsync();

            return new PagedResponse<BloodSugarRecord>(items, totalCount, request.Page, request.PageSize);
        }

        public async Task<List<BloodSugarRecord>> GetByCareGroupIdAndDateRangeAsync(Guid careGroupId, DateTime dateFrom, DateTime dateTo)
        {
            return await _context.Set<BloodSugarRecord>()
                .Where(x => x.CareGroupId == careGroupId && x.RecordDate >= dateFrom && x.RecordDate <= dateTo)
                .OrderBy(x => x.RecordDate)
                .ToListAsync();
        }

        public async Task<BloodSugarRecord?> GetByIdAsync(Guid careGroupId, Guid id)
        {
            return await _context.Set<BloodSugarRecord>()
                .FirstOrDefaultAsync(x => x.CareGroupId == careGroupId && x.Id == id);
        }

        public async Task<BloodSugarRecord> AddAsync(BloodSugarRecord record)
        {
            // 單純宣告向 EF Core 的追蹤器 (Tracker) 註冊這筆新增
            _context.Set<BloodSugarRecord>().Add(record);
            // 將上述變更 (工作單元)，一次性送交指令給資料庫
            await _context.SaveChangesAsync();
            return record;
        }

        public async Task<BloodSugarRecord> UpdateAsync(BloodSugarRecord record)
        {
            _context.Set<BloodSugarRecord>().Update(record);
            await _context.SaveChangesAsync();
            return record;
        }
    }
}
