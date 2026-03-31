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
    public class TemperatureRepository : ITemperatureRepository
    {
        private readonly DbContext _context;

        public TemperatureRepository(DbContext context)
        {
            _context = context;
        }

        public async Task<PagedResponse<TemperatureRecord>> GetPagedAsync(Guid careGroupId, PaginationRequest request)
        {
            var query = _context.Set<TemperatureRecord>()
                .Where(x => x.CareGroupId == careGroupId)
                .OrderByDescending(x => x.RecordDate);

            var totalCount = await query.CountAsync();
            var items = await query
                .Skip((request.Page - 1) * request.PageSize)
                .Take(request.PageSize)
                .ToListAsync();

            return new PagedResponse<TemperatureRecord>(items, totalCount, request.Page, request.PageSize);
        }

        public async Task<TemperatureRecord?> GetByIdAsync(Guid careGroupId, Guid id)
        {
            return await _context.Set<TemperatureRecord>()
                .FirstOrDefaultAsync(x => x.CareGroupId == careGroupId && x.Id == id);
        }

        public async Task<TemperatureRecord> AddAsync(TemperatureRecord record)
        {
            // 單純宣告向 EF Core 的追蹤器 (Tracker) 註冊這筆新增
            _context.Set<TemperatureRecord>().Add(record);
            // 將上述變更 (工作單元)，一次性送交指令給資料庫
            await _context.SaveChangesAsync();
            return record;
        }

        public async Task<TemperatureRecord> UpdateAsync(TemperatureRecord record)
        {
            _context.Set<TemperatureRecord>().Update(record);
            await _context.SaveChangesAsync();
            return record;
        }
    }
}
