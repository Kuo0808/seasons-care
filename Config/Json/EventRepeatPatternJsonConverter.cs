using System;
using System.Text.Json;
using System.Text.Json.Serialization;
using SeasonsCare.Api.Models.Enums;

namespace SeasonsCare.Api.Config.Json
{
    /// <summary>
    /// 讓前端可以用字串值與 API 溝通重複規則，
    /// 其中 weeklyDay 會映射到內部的 Weekly。
    /// </summary>
    public class EventRepeatPatternJsonConverter : JsonConverter<EventRepeatPattern>
    {
        public override EventRepeatPattern Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType == JsonTokenType.String)
            {
                var value = reader.GetString();
                return value?.Trim() switch
                {
                    "none" => EventRepeatPattern.None,
                    "daily" => EventRepeatPattern.Daily,
                    "weekly" => EventRepeatPattern.Weekly,
                    "weeklyDay" => EventRepeatPattern.Weekly,
                    "monthly" => EventRepeatPattern.Monthly,
                    _ => throw new JsonException($"Unsupported repeatPattern value: {value}")
                };
            }

            if (reader.TokenType == JsonTokenType.Number &&
                reader.TryGetInt32(out var numericValue) &&
                Enum.IsDefined(typeof(EventRepeatPattern), numericValue))
            {
                return (EventRepeatPattern)numericValue;
            }

            throw new JsonException("repeatPattern must be a valid string or numeric enum value.");
        }

        public override void Write(Utf8JsonWriter writer, EventRepeatPattern value, JsonSerializerOptions options)
        {
            var serializedValue = value switch
            {
                EventRepeatPattern.None => "none",
                EventRepeatPattern.Daily => "daily",
                EventRepeatPattern.Weekly => "weeklyDay",
                EventRepeatPattern.Monthly => "monthly",
                _ => throw new JsonException($"Unsupported repeatPattern enum value: {value}")
            };

            writer.WriteStringValue(serializedValue);
        }
    }
}
