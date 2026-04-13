namespace SeasonsCare.Api.DTOs.Common
{
    public class PaginationRequest
    {
        /// <summary>
        /// 選填。頁碼，預設為 1。
        /// </summary>
        public int Page { get; set; } = 1;

        /// <summary>
        /// 選填。每頁筆數，預設為 20。
        /// </summary>
        public int PageSize { get; set; } = 20;

        /// <summary>
        /// 選填。排序欄位，預設為 createdAt_desc。
        /// </summary>
        public string Sort { get; set; } = "createdAt_desc";
    }
}
