using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using SeasonsCare.Api.Data;
using SeasonsCare.Api.Models.Entities;

namespace SeasonsCare.Api.Repositories
{
    public class ExpenseSplitRepository : IExpenseSplitRepository
    {
        private readonly ApplicationDbContext _context;

        public ExpenseSplitRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task AddRangeAsync(IEnumerable<ExpenseSplit> splits)
        {
            await _context.ExpenseSplits.AddRangeAsync(splits);
        }

        public async Task<List<ExpenseSplit>> GetByCareGroupAsync(Guid careGroupId, DateTime? dateFrom, DateTime? dateTo)
        {
            // share 模式累計時要看「分攤對象 (UserId)」，但日期範圍仍以原 Expense 的 ExpenseDate 為準，
            // 才能與 payer 模式語義對齊（同一個區間 / 同一筆費用）。
            var query = _context.ExpenseSplits
                .Include(s => s.Expense)
                .Where(s => s.CareGroupId == careGroupId && s.DeletedAt == null);

            if (dateFrom.HasValue)
            {
                query = query.Where(s => s.Expense.ExpenseDate >= dateFrom.Value);
            }
            if (dateTo.HasValue)
            {
                query = query.Where(s => s.Expense.ExpenseDate < dateTo.Value);
            }

            return await query.ToListAsync();
        }

        public async Task SaveChangesAsync()
        {
            await _context.SaveChangesAsync();
        }
    }
}
