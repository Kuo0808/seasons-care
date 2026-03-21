using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using SeasonsCare.Api.Models.Entities;

namespace SeasonsCare.Api.Repositories
{
    public interface IExpenseRepository
    {
        Task<ExpenseRecord?> GetByIdAsync(Guid id);
        Task<(List<ExpenseRecord> Data, int TotalCount)> GetPagedByCareGroupIdAsync(Guid careGroupId, int page, int pageSize, string sort);
        Task AddAsync(ExpenseRecord expense);
        Task UpdateAsync(ExpenseRecord expense);
        Task SaveChangesAsync();
    }
}
