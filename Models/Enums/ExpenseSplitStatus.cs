namespace SeasonsCare.Api.Models.Enums
{
    /// <summary>
    /// 支出分帳狀態。
    /// </summary>
    public enum ExpenseSplitStatus
    {
        /// <summary>
        /// 尚待分帳或付款。
        /// </summary>
        Pending,

        /// <summary>
        /// 已完成分帳或付款。
        /// </summary>
        Settled,

        /// <summary>
        /// 不需要分帳。
        /// </summary>
        None
    }
}
