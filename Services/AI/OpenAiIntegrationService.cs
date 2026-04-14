using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using SeasonsCare.Api.Config;
using SeasonsCare.Api.Config.OpenAI;
using SeasonsCare.Api.DTOs.HealthDashboard;

namespace SeasonsCare.Api.Services.AI
{
    public class OpenAiIntegrationService : IAiIntegrationService
    {
        private const string PromptVersion = "health-dashboard-v4";
        private const int MaxRetryAttempts = 3;
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

            var payloadJson = JsonSerializer.Serialize(BuildPromptPayload(model, input), JsonOptions);
            var responseContent = await SendWithRetryAsync(apiKey, payloadJson);

            using var json = JsonDocument.Parse(responseContent);
            var outputText = ExtractOutputText(json.RootElement);
            if (string.IsNullOrWhiteSpace(outputText))
            {
                throw new InvalidOperationException("OpenAI response did not contain structured output text.");
            }

            using var outputJson = JsonDocument.Parse(outputText);

            TrendLabelsDto? trendLabels = null;
            if (outputJson.RootElement.TryGetProperty("trendLabels", out var trendLabelsElement))
            {
                trendLabels = JsonSerializer.Deserialize<TrendLabelsDto>(trendLabelsElement.GetRawText(), JsonOptions);
            }

            return new AiGeneratedInsightDto
            {
                OverallSummary = outputJson.RootElement.GetProperty("overallSummary").GetString() ?? string.Empty,
                TodaySummary = outputJson.RootElement.GetProperty("todaySummary").GetString() ?? string.Empty,
                KeyInsights = outputJson.RootElement.GetProperty("keyInsights").GetString() ?? string.Empty,
                Recommendations = outputJson.RootElement.GetProperty("recommendations").GetString() ?? string.Empty,
                TrendLabels = trendLabels,
                SourceDataHash = ComputeSourceDataHash(input),
                ModelName = model,
                PromptVersion = PromptVersion,
                GeneratedAt = TimeHelper.UtcNow
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
Rules:
- 語氣要求 (Tone): 必須保持溫暖、同理心、且充滿鼓勵的語氣，就像親切的護理師。可以適當加入例如「再加油一點點！」、「辛苦了！」等鼓勵性用語。
- overallSummary (AI分析報告): 總結過去7天的健康狀況趨勢，需控制在50個中文字以內。例如：「健康狀況在過去7天內呈現正面趨勢。血壓已完全進入理想區間，體重管理效果顯著。」
- keyInsights (關鍵數據洞察): 指出最重要的數據波動或異常，需控制在50個中文字以內。例如：「血糖水平在飯後有輕微波動（+8%），主要集中在週三及週五。」
- recommendations (健康行動建議): 針對異常數據或維持健康給出具體且生活化的建議（飲食或作息）。例如：「為維持穩定血糖，建議將澱粉攝取量減少15%，並持續目前的低鈉飲食以保護已趨穩定的血壓指標。」
- todaySummary (今日分析摘要): 給予首頁卡片的今日簡短回饋。如果今日有數據完成，給予肯定或提醒，加上一句鼓勵的話。例如：「下午已完成血壓測量，數值偏高，建議傍晚減少咖啡因攝取。今日復健進度已達成80%，再加油一點點！」如果今日完全沒有數據，請回傳「當日尚未有紀錄，快來新增吧！」。字數限制約50字內。
""";

            return new
            {
                model,
                instructions = "You are a warm, empathetic, and professional healthcare assistant for caregivers. Provide factual but encouraging responses, sounding like a caring nurse. Use Traditional Chinese.",
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
                                    description = "A short overall summary of the last 7 days in a warm professional tone. Max 50 characters."
                                },
                                todaySummary = new
                                {
                                    type = "string",
                                    description = "A concise, actionable, and encouraging insight for today. Max 50 characters. E.g., '今日已完成O項任務...再加油一點點！'"
                                },
                                keyInsights = new
                                {
                                    type = "string",
                                    description = "Important observations the caregiver should notice. Max 50 characters."
                                },
                                recommendations = new
                                {
                                    type = "string",
                                    description = "Actionable suggestions focused on diet and lifestyle habits."
                                },
                                trendLabels = new
                                {
                                    type = "object",
                                    description = "Short status label for each health metric trend, such as 正常、穩定、需要觀察 or 資料不足. Use 資料不足 when there is no data.",
                                    additionalProperties = false,
                                    properties = new
                                    {
                                        bloodPressure = new { type = "string" },
                                        bloodOxygen = new { type = "string" },
                                        bloodSugar = new { type = "string" },
                                        temperature = new { type = "string" },
                                        weight = new { type = "string" }
                                    },
                                    required = new[] { "bloodPressure", "bloodOxygen", "bloodSugar", "temperature", "weight" }
                                }
                            },
                            required = new[] { "overallSummary", "todaySummary", "keyInsights", "recommendations", "trendLabels" }
                        }
                    }
                }
            };
        }

        private async Task<string> SendWithRetryAsync(string apiKey, string payloadJson)
        {
            string? lastFailure = null;

            for (var attempt = 1; attempt <= MaxRetryAttempts; attempt++)
            {
                using var request = new HttpRequestMessage(HttpMethod.Post, "https://api.openai.com/v1/responses");
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
                request.Content = new StringContent(payloadJson, Encoding.UTF8, "application/json");

                using var response = await _httpClient.SendAsync(request);
                var responseContent = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    return responseContent;
                }

                lastFailure = $"OpenAI request failed with status {(int)response.StatusCode}: {responseContent}";
                if (attempt == MaxRetryAttempts || !IsTransientStatusCode(response.StatusCode))
                {
                    throw new InvalidOperationException(lastFailure);
                }

                var delay = response.Headers.RetryAfter?.Delta ?? TimeSpan.FromSeconds(Math.Pow(2, attempt - 1));
                await Task.Delay(delay);
            }

            throw new InvalidOperationException(lastFailure ?? "OpenAI request failed for an unknown reason.");
        }

        private static bool IsTransientStatusCode(HttpStatusCode statusCode)
        {
            return statusCode == HttpStatusCode.RequestTimeout
                || statusCode == HttpStatusCode.TooManyRequests
                || statusCode == HttpStatusCode.InternalServerError
                || statusCode == HttpStatusCode.BadGateway
                || statusCode == HttpStatusCode.ServiceUnavailable
                || statusCode == HttpStatusCode.GatewayTimeout;
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
