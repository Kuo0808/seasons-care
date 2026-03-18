using System.Collections.Generic;

namespace SeasonsCare.Api.DTOs.Common
{
    public class PagedResponse<T>
    {
        public IEnumerable<T> Items { get; set; } = new List<T>();
        public PaginationMeta Pagination { get; set; } = new PaginationMeta();

        public PagedResponse() { }

        public PagedResponse(IEnumerable<T> items, int totalCount, int currentPage, int pageSize)
        {
            Items = items;
            Pagination = new PaginationMeta
            {
                TotalCount = totalCount,
                CurrentPage = currentPage,
                PageSize = pageSize,
                TotalPages = pageSize > 0 ? (int)System.Math.Ceiling(totalCount / (double)pageSize) : 0
            };
        }
    }

    public class PaginationMeta
    {
        public int TotalCount { get; set; }
        public int TotalPages { get; set; }
        public int CurrentPage { get; set; }
        public int PageSize { get; set; }
    }
}
