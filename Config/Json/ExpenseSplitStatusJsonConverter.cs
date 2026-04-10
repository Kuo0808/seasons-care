using System;
using System.Text.Json;
using System.Text.Json.Serialization;
using SeasonsCare.Api.Models.Enums;

namespace SeasonsCare.Api.Config.Json
{
    public class ExpenseSplitStatusJsonConverter : JsonConverter<ExpenseSplitStatus>
    {
        public override ExpenseSplitStatus Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType == JsonTokenType.String)
            {
                var value = reader.GetString();
                return value?.Trim() switch
                {
                    "pending" => ExpenseSplitStatus.Pending,
                    "settled" => ExpenseSplitStatus.Settled,
                    "none" => ExpenseSplitStatus.None,
                    _ => throw new JsonException($"Unsupported splitStatus value: {value}")
                };
            }

            if (reader.TokenType == JsonTokenType.Number &&
                reader.TryGetInt32(out var numericValue) &&
                Enum.IsDefined(typeof(ExpenseSplitStatus), numericValue))
            {
                return (ExpenseSplitStatus)numericValue;
            }

            throw new JsonException("splitStatus must be a valid string or numeric enum value.");
        }

        public override void Write(Utf8JsonWriter writer, ExpenseSplitStatus value, JsonSerializerOptions options)
        {
            var serializedValue = value switch
            {
                ExpenseSplitStatus.Pending => "pending",
                ExpenseSplitStatus.Settled => "settled",
                ExpenseSplitStatus.None => "none",
                _ => throw new JsonException($"Unsupported splitStatus enum value: {value}")
            };

            writer.WriteStringValue(serializedValue);
        }
    }
}
