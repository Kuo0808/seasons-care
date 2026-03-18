namespace SeasonsCare.Api.DTOs.Common
{
    public class PaginationRequest
    {
        public int Page { get; set; } = 1;

        public int PageSize { get; set; } = 20;

        public string Sort { get; set; } = "createdAt_desc";
    }
}
