using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using SeasonsCare.Api.Models.Entities;

namespace SeasonsCare.Api.Repositories
{
    /// <summary>
    /// 分帳明細 (ExpenseSplit) 的資料存取介面。
    /// </summary>
    public interface IExpenseSplitRepository
    {
        /// <summary>
        /// 一次寫入多筆分帳明細（單筆 expense 結算時會展開為 N 筆 split）。
        /// </summary>
        Task AddRangeAsync(IEnumerable<ExpenseSplit> splits);

        /// <summary>
        /// 取得某 careGroup 的分帳明細，可選日期區間（以 ExpenseRecord.ExpenseDate 為準）。
        /// dateFrom / dateTo 任一為 null 則不套用該邊界。
        /// </summary>
        Task<List<ExpenseSplit>> GetByCareGroupAsync(Guid careGroupId, DateTime? dateFrom, DateTime? dateTo);

        /// <summary>
        /// 依 SplitBatchId 取得整批分帳明細（含對應 ExpenseRecord）。
        /// 用於通知點擊後回傳「已分帳結果」彈窗資料。
        /// </summary>
        Task<List<ExpenseSplit>> GetByBatchIdAsync(Guid careGroupId, Guid splitBatchId);

        Task SaveChangesAsync();
    }
}
