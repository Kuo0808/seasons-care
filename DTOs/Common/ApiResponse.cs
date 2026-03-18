using System.Text.Json.Serialization;

namespace SeasonsCare.Api.DTOs.Common
{
    // ApiResponse<T> 僅用於「成功回應」
    public class ApiResponse<T>
    {
        public bool Success { get; set; } = true;
        
        public string Message { get; set; } = string.Empty;
        
        public T? Data { get; set; }
        
        public string TraceId { get; set; } = string.Empty;

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public PaginationMeta? Pagination { get; set; }

        public ApiResponse(T? data, string message = "", string traceId = "", PaginationMeta? pagination = null)
        {
            Data = data;
            Message = message;
            TraceId = traceId;
            Pagination = pagination;
        }
    }
}
