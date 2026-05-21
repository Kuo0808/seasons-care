using System;
using System.Text.Json;

namespace SeasonsCare.Api.DTOs.Notifications
{
    public class NotificationResponse
    {
        public Guid Id { get; set; }

        public Guid CareGroupId { get; set; }

        public string Type { get; set; } = string.Empty;

        public string Title { get; set; } = string.Empty;

        public string Message { get; set; } = string.Empty;

        public bool IsRead { get; set; }

        public DateTimeOffset? ReadAt { get; set; }

        public DateTimeOffset CreatedAt { get; set; }

        public JsonElement? Payload { get; set; }
    }
}
