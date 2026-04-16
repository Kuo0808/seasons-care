using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using SeasonsCare.Api.Data;
using SeasonsCare.Api.DTOs.Common;
using SeasonsCare.Api.Models.Entities;

namespace SeasonsCare.Api.Repositories
{
    // [架構導覽] 資料存取層 (Data Access Layer) - Repository
    // 職責：隔離對底層資料庫的直接依賴。封裝 O/RM (Entity Framework Core) 語法，與資料庫進行實際交涉。不應含有 if/else 的業務權限判斷。
    public class ExpenseRepository : IExpenseRepository
    {
        private readonly ApplicationDbContext _context;

        public ExpenseRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<ExpenseRecord?> GetByIdAsync(Guid id)
        {
            return await _context.Expenses.FirstOrDefaultAsync(e => e.Id == id);
        }

        public async Task<List<ExpenseRecord>> GetListByIdsAsync(Guid careGroupId, IEnumerable<Guid> expenseIds)
        {
            return await _context.Expenses
                .Where(e => e.CareGroupId == careGroupId && expenseIds.Contains(e.Id))
                .ToListAsync();
        }

        public async Task<(List<ExpenseRecord> Data, int TotalCount)> GetPagedByCareGroupIdAsync(Guid careGroupId, int page, int pageSize, string sort)
        {
            // 以業務日期作為主時間軸，避免前端一次抓取全量歷史資料。
            var query = _context.Expenses.Where(e => e.CareGroupId == careGroupId);

            var totalCount = await query.CountAsync();

            query = sort switch
            {
                "expenseDate_asc" => query.OrderBy(e => e.ExpenseDate),
                "expenseDate_desc" => query.OrderByDescending(e => e.ExpenseDate),
                "amount_asc" => query.OrderBy(e => e.Amount),
                "amount_desc" => query.OrderByDescending(e => e.Amount),
                _ => query.OrderByDescending(e => e.CreatedAt)
            };

            var data = await query.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();

            return (data, totalCount);
        }

        public async Task<(List<ExpenseRecord> Data, int TotalCount)> GetPagedByCareGroupIdAsync(Guid careGroupId, PaginationRequest request)
        {
            var query = _context.Expenses
                .Where(e => e.CareGroupId == careGroupId);

            query = ApplyExpenseDateRange(query, request);

            var totalCount = await query.CountAsync();

            query = request.Sort switch
            {
                "expenseDate_asc" => query.OrderBy(e => e.ExpenseDate),
                "expenseDate_desc" => query.OrderByDescending(e => e.ExpenseDate),
                "amount_asc" => query.OrderBy(e => e.Amount),
                "amount_desc" => query.OrderByDescending(e => e.Amount),
                "createdAt_asc" => query.OrderBy(e => e.CreatedAt),
                "createdAt_desc" => query.OrderByDescending(e => e.CreatedAt),
                _ => query.OrderByDescending(e => e.ExpenseDate)
            };

            var data = await query
                .Skip((request.Page - 1) * request.PageSize)
                .Take(request.PageSize)
                .ToListAsync();

            return (data, totalCount);
        }

        private static IQueryable<ExpenseRecord> ApplyExpenseDateRange(IQueryable<ExpenseRecord> query, PaginationRequest request)
        {
            var rangeRequest = request.ToDateRangeRequest();

            if (rangeRequest.StartDate.HasValue || rangeRequest.EndDate.HasValue)
            {
                return query.ApplyDateRange(request, e => e.ExpenseDate);
            }

            var utcToday = DateTime.UtcNow.Date;
            var monthStartUtc = new DateTime(utcToday.Year, utcToday.Month, 1, 0, 0, 0, DateTimeKind.Utc);
            var nextMonthStartUtc = monthStartUtc.AddMonths(1);

            return query.Where(e => e.ExpenseDate >= monthStartUtc && e.ExpenseDate < nextMonthStartUtc);
        }

        public async Task<List<ExpenseRecord>> GetPendingByDateRangeAsync(Guid careGroupId, DateTime dateFrom, DateTime dateTo)
        {
            // 分帳僅處理 Pending（待分帳）狀態，排除 None（無需分帳）與 Settled（已結算）。
            return await _context.Expenses
                .Where(e => e.CareGroupId == careGroupId
                    && e.ExpenseDate >= dateFrom
                    && e.ExpenseDate < dateTo
                    && e.SplitStatus == Models.Enums.ExpenseSplitStatus.Pending
                    && e.DeletedAt == null)
                .OrderBy(e => e.ExpenseDate)
                .ToListAsync();
        }

        public async Task<List<ExpenseRecord>> GetByCareGroupAsync(Guid careGroupId, DateTime? dateFrom, DateTime? dateTo, Models.Enums.ExpenseSplitStatus? status)
        {
            var query = _context.Expenses
                .Where(e => e.CareGroupId == careGroupId && e.DeletedAt == null);

            if (dateFrom.HasValue)
            {
                query = query.Where(e => e.ExpenseDate >= dateFrom.Value);
            }
            if (dateTo.HasValue)
            {
                query = query.Where(e => e.ExpenseDate < dateTo.Value);
            }
            if (status.HasValue)
            {
                query = query.Where(e => e.SplitStatus == status.Value);
            }

            return await query.OrderBy(e => e.ExpenseDate).ToListAsync();
        }

        public async Task AddAsync(ExpenseRecord expense)
        {
            // 單純宣告向 EF Core 的追蹤器 (Tracker) 註冊這筆新增，但不馬上異動資料庫實體
            await _context.Expenses.AddAsync(expense);
        }

        public async Task UpdateAsync(ExpenseRecord expense)
        {
            _context.Expenses.Update(expense);
            await Task.CompletedTask;
        }

        public async Task SaveChangesAsync()
        {
            // 將上述所有追蹤到的變更 (工作單元)，一次性送交指令給資料庫
            await _context.SaveChangesAsync();
        }
    }
}
