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
    public class WeightRepository : IWeightRepository
    {
        private readonly DbContext _context;

        public WeightRepository(DbContext context)
        {
            _context = context;
        }

        public async Task<PagedResponse<WeightRecord>> GetPagedAsync(Guid careGroupId, PaginationRequest request)
        {
            var query = _context.Set<WeightRecord>()
                .Where(x => x.CareGroupId == careGroupId)
                .ApplyDateRange(request, x => x.RecordDate);

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

            return new PagedResponse<WeightRecord>(items, totalCount, request.Page, request.PageSize);
        }

        public async Task<List<WeightRecord>> GetByCareGroupIdAndDateRangeAsync(Guid careGroupId, DateTime dateFrom, DateTime dateTo)
        {
            return await _context.Set<WeightRecord>()
                .Where(x => x.CareGroupId == careGroupId && x.RecordDate >= dateFrom && x.RecordDate <= dateTo)
                .OrderBy(x => x.RecordDate)
                .ToListAsync();
        }

        public async Task<WeightRecord?> GetByIdAsync(Guid careGroupId, Guid id)
        {
            return await _context.Set<WeightRecord>()
                .FirstOrDefaultAsync(x => x.CareGroupId == careGroupId && x.Id == id);
        }

        public async Task<WeightRecord> AddAsync(WeightRecord record)
        {
            // 單純宣告向 EF Core 的追蹤器 (Tracker) 註冊這筆新增
            _context.Set<WeightRecord>().Add(record);
            // 將上述變更 (工作單元)，一次性送交指令給資料庫
            await _context.SaveChangesAsync();
            return record;
        }

        public async Task<WeightRecord> UpdateAsync(WeightRecord record)
        {
            _context.Set<WeightRecord>().Update(record);
            await _context.SaveChangesAsync();
            return record;
        }
    }
}
