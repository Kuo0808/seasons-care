using System;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using SeasonsCare.Api.Config.OpenAI;
using SeasonsCare.Api.DTOs.HealthDashboard;

namespace SeasonsCare.Api.Services.AI
{
    public class OpenAiIntegrationService : IAiIntegrationService
    {
        private const string PromptVersion = "health-dashboard-v1";
        private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

        private readonly HttpClient _httpClient;
        private readonly IOptions<OpenAiOptions> _options;

        public OpenAiIntegrationService(HttpClient httpClient, IOptions<OpenAiOptions> options)
        {
            _httpClient = httpClient;
            _options = options;
        }

        public async Task<AiGeneratedInsightDto> GenerateHealthInsightAsync(HealthInsightPromptInput input)
        {
            var apiKey = _options.Value.ApiKey;
            var model = string.IsNullOrWhiteSpace(_options.Value.Model) ? "gpt-4o-mini" : _options.Value.Model;

            if (string.IsNullOrWhiteSpace(apiKey))
            {
                throw new InvalidOperationException("OpenAI API key is not configured.");
            }

            using var request = new HttpRequestMessage(HttpMethod.Post, "https://api.openai.com/v1/responses");
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
            request.Content = new StringContent(
                JsonSerializer.Serialize(BuildPromptPayload(model, input), JsonOptions),
                Encoding.UTF8,
                "application/json");

            using var response = await _httpClient.SendAsync(request);
            var responseContent = await response.Content.ReadAsStringAsync();
            if (!response.IsSuccessStatusCode)
            {
                throw new InvalidOperationException($"OpenAI request failed with status {(int)response.StatusCode}: {responseContent}");
            }

            using var json = JsonDocument.Parse(responseContent);
            var outputText = ExtractOutputText(json.RootElement);
            if (string.IsNullOrWhiteSpace(outputText))
            {
                throw new InvalidOperationException("OpenAI response did not contain structured output text.");
            }

            using var outputJson = JsonDocument.Parse(outputText);
            return new AiGeneratedInsightDto
            {
                OverallSummary = outputJson.RootElement.GetProperty("overallSummary").GetString() ?? string.Empty,
                KeyInsights = outputJson.RootElement.GetProperty("keyInsights").GetString() ?? string.Empty,
                Recommendations = outputJson.RootElement.GetProperty("recommendations").GetString() ?? string.Empty,
                SourceDataHash = ComputeSourceDataHash(input),
                ModelName = model,
                PromptVersion = PromptVersion,
                GeneratedAt = DateTime.UtcNow
            };
        }

        private static object BuildPromptPayload(string model, HealthInsightPromptInput input)
        {
            var prompt = $"""
Analyze the following 7-day health dashboard summary for one care group and respond in Traditional Chinese.

Date range: {input.DateFrom:yyyy-MM-dd} to {input.DateTo:yyyy-MM-dd}
Today's summary:
{input.TodaySummary}

Blood pressure:
{input.BloodPressureSummary}

Blood sugar:
{input.BloodSugarSummary}

Weight:
{input.WeightSummary}

Temperature:
{input.TemperatureSummary}

Blood oxygen:
{input.BloodOxygenSummary}

Write concise, practical guidance for caregivers. Use only the supplied data.
""";

            return new
            {
                model,
                instructions = "You are a careful health trend summarization assistant for caregivers. Keep the response factual, concise, and supportive.",
                input = new object[]
                {
                    new
                    {
                        role = "user",
                        content = new object[]
                        {
                            new
                            {
                                type = "input_text",
                                text = prompt
                            }
                        }
                    }
                },
                text = new
                {
                    format = new
                    {
                        type = "json_schema",
                        name = "health_dashboard_report",
                        strict = true,
                        schema = new
                        {
                            type = "object",
                            additionalProperties = false,
                            properties = new
                            {
                                overallSummary = new
                                {
                                    type = "string",
                                    description = "A short overall summary of the last 7 days."
                                },
                                keyInsights = new
                                {
                                    type = "string",
                                    description = "Important observations the caregiver should notice."
                                },
                                recommendations = new
                                {
                                    type = "string",
                                    description = "Actionable next-step suggestions for the caregiver."
                                }
                            },
                            required = new[] { "overallSummary", "keyInsights", "recommendations" }
                        }
                    }
                }
            };
        }

        private static string ExtractOutputText(JsonElement root)
        {
            if (root.TryGetProperty("output_text", out var outputTextElement) && outputTextElement.ValueKind == JsonValueKind.String)
            {
                return outputTextElement.GetString() ?? string.Empty;
            }

            if (!root.TryGetProperty("output", out var outputArray) || outputArray.ValueKind != JsonValueKind.Array)
            {
                return string.Empty;
            }

            foreach (var message in outputArray.EnumerateArray().Where(x => x.TryGetProperty("type", out var type) && type.GetString() == "message"))
            {
                if (!message.TryGetProperty("content", out var contentArray) || contentArray.ValueKind != JsonValueKind.Array)
                {
                    continue;
                }

                foreach (var content in contentArray.EnumerateArray())
                {
                    if (!content.TryGetProperty("type", out var contentType))
                    {
                        continue;
                    }

                    var typeValue = contentType.GetString();
                    if (typeValue == "output_text" && content.TryGetProperty("text", out var textElement))
                    {
                        return textElement.GetString() ?? string.Empty;
                    }

                    if (typeValue == "refusal" && content.TryGetProperty("refusal", out var refusalElement))
                    {
                        throw new InvalidOperationException($"OpenAI refused to generate a health dashboard insight: {refusalElement.GetString()}");
                    }
                }
            }

            return string.Empty;
        }

        private static string ComputeSourceDataHash(HealthInsightPromptInput input)
        {
            var serialized = JsonSerializer.Serialize(input, JsonOptions);
            var hash = SHA256.HashData(Encoding.UTF8.GetBytes(serialized));
            return Convert.ToHexString(hash).ToLowerInvariant();
        }
    }
}
