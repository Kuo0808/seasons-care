using System;

namespace SeasonsCare.Api.DTOs.CareLogs
{
    public class CareLogResponse
    {
        public Guid Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string? Content { get; set; }
        public string? LogType { get; set; }
        public DateTime RecordDate { get; set; }
        public Guid CareGroupId { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public string CreatedBy { get; set; } = string.Empty;
    }
}
