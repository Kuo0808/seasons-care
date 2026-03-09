using System;

namespace SeasonsCare.Api.DTOs.Common
{
    // ApiResponse<T> 僅用於「成功回應」
    public class ApiResponse<T>
    {
        public bool Success { get; set; } = true;
        
        public string Message { get; set; }
        
        public T Data { get; set; }
        
        public string TraceId { get; set; }

        public ApiResponse(T data, string message = "", string traceId = "")
        {
            Data = data;
            Message = message;
            TraceId = traceId;
        }
    }
}
