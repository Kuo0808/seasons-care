using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using SeasonsCare.Api.DTOs.Common;
using SeasonsCare.Api.Models.Entities;
using SeasonsCare.Api.Models.Enums;

namespace SeasonsCare.Api.Repositories
{
    public interface IExpenseRepository
    {
        Task<ExpenseRecord?> GetByIdAsync(Guid id);
        Task<(List<ExpenseRecord> Data, int TotalCount)> GetPagedByCareGroupIdAsync(Guid careGroupId, int page, int pageSize, string sort);
        Task<(List<ExpenseRecord> Data, int TotalCount)> GetPagedByCareGroupIdAsync(Guid careGroupId, PaginationRequest request);
        Task<List<ExpenseRecord>> GetListByIdsAsync(Guid careGroupId, IEnumerable<Guid> expenseIds);
        Task<List<ExpenseRecord>> GetPendingByDateRangeAsync(Guid careGroupId, DateTime dateFrom, DateTime dateTo);
        /// <summary>
        /// 依照 CareGroup、可選的日期範圍與可選的分帳狀態取得費用清單。
        /// dateFrom / dateTo 任一為 null 則不套用該邊界；status 為 null 則不套用狀態篩選。
        /// </summary>
        Task<List<ExpenseRecord>> GetByCareGroupAsync(Guid careGroupId, DateTime? dateFrom, DateTime? dateTo, ExpenseSplitStatus? status);
        Task AddAsync(ExpenseRecord expense);
        Task UpdateAsync(ExpenseRecord expense);
        Task SaveChangesAsync();
    }
}
