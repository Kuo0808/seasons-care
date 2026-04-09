using System;
using System.Text.Json;
using System.Text.Json.Serialization;
using SeasonsCare.Api.Models.Enums;

namespace SeasonsCare.Api.Config.Json
{
    /// <summary>
    /// 讓事件實例狀態以較貼近前端語意的字串進行序列化與反序列化。
    /// </summary>
    public class EventOccurrenceStatusJsonConverter : JsonConverter<EventOccurrenceStatus>
    {
        public override EventOccurrenceStatus Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType == JsonTokenType.String)
            {
                var value = reader.GetString();
                return value?.Trim() switch
                {
                    "pending" => EventOccurrenceStatus.Scheduled,
                    "scheduled" => EventOccurrenceStatus.Scheduled,
                    "completed" => EventOccurrenceStatus.Completed,
                    "cancelled" => EventOccurrenceStatus.Cancelled,
                    "skipped" => EventOccurrenceStatus.Skipped,
                    _ => throw new JsonException($"Unsupported occurrence status: {value}")
                };
            }

            if (reader.TokenType == JsonTokenType.Number &&
                reader.TryGetInt32(out var numericValue) &&
                Enum.IsDefined(typeof(EventOccurrenceStatus), numericValue))
            {
                return (EventOccurrenceStatus)numericValue;
            }

            throw new JsonException("status must be a valid string or numeric enum value.");
        }

        public override void Write(Utf8JsonWriter writer, EventOccurrenceStatus value, JsonSerializerOptions options)
        {
            var serializedValue = value switch
            {
                EventOccurrenceStatus.Scheduled => "pending",
                EventOccurrenceStatus.Completed => "completed",
                EventOccurrenceStatus.Cancelled => "cancelled",
                EventOccurrenceStatus.Skipped => "skipped",
                _ => throw new JsonException($"Unsupported occurrence status enum value: {value}")
            };

            writer.WriteStringValue(serializedValue);
        }
    }
}
