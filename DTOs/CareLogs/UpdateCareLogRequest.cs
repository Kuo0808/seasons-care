using System;

namespace SeasonsCare.Api.DTOs.CareLogs
{
    public class UpdateCareLogRequest
    {
        public string Title { get; set; } = string.Empty;
        public string? Content { get; set; }
        public string? LogType { get; set; }
        public DateTime? RecordDate { get; set; }
        public DateTime? UpdatedAt { get; set; } // for optimistic concurrency
    }
}
