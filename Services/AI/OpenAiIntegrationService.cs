using System;
using System.Collections.Generic;
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
        private const string PromptVersion = "health-dashboard-v6";
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
            var root = outputJson.RootElement;

            var heroReport = DeserializeSection<HealthDashboardHeroReportDto>(root, "heroReport") ?? new HealthDashboardHeroReportDto();
            var keyInsightSection = DeserializeSection<HealthDashboardInsightSectionDto>(root, "keyInsight") ?? new HealthDashboardInsightSectionDto();
            var actionSuggestionSection = DeserializeSection<HealthDashboardActionSectionDto>(root, "actionSuggestion") ?? new HealthDashboardActionSectionDto();
            var trendLabels = DeserializeSection<TrendLabelsDto>(root, "trendLabels");
            var todayCards = DeserializeList<HealthDashboardTodayCardDto>(root, "todayCards");
            var alerts = DeserializeList<HealthDashboardAlertDto>(root, "alerts");

            var legacyOverallSummary = FirstNonEmpty(
                root.GetProperty("overallSummary").GetString(),
                heroReport.Headline,
                heroReport.Body);
            var legacyTodaySummary = FirstNonEmpty(
                root.GetProperty("todaySummary").GetString(),
                todayCards.FirstOrDefault()?.Summary,
                heroReport.Body);
            var legacyKeyInsights = FirstNonEmpty(
                root.GetProperty("keyInsights").GetString(),
                keyInsightSection.Body,
                heroReport.Body);
            var legacyRecommendations = FirstNonEmpty(
                root.GetProperty("recommendations").GetString(),
                actionSuggestionSection.Body);

            return new AiGeneratedInsightDto
            {
                OverallSummary = legacyOverallSummary,
                TodaySummary = legacyTodaySummary,
                KeyInsights = legacyKeyInsights,
                Recommendations = legacyRecommendations,
                TrendLabels = trendLabels,
                HeroReport = heroReport,
                KeyInsightSection = keyInsightSection,
                ActionSuggestionSection = actionSuggestionSection,
                TodayCards = todayCards,
                Alerts = alerts,
                SourceDataHash = ComputeSourceDataHash(input),
                ModelName = model,
                PromptVersion = PromptVersion,
                GeneratedAt = TimeHelper.UtcNow
            };
        }

        private static object BuildPromptPayload(string model, HealthInsightPromptInput input)
        {
            var facts = new
            {
                dateRange = new
                {
                    from = input.DateFrom.ToString("yyyy-MM-dd"),
                    to = input.DateTo.ToString("yyyy-MM-dd")
                },
                totalRecordCount = input.TotalRecordCount,
                todayRecordCount = input.TodayRecordCount,
                todaySummary = input.TodaySummary,
                metrics = new Dictionary<string, string>
                {
                    ["bloodPressure"] = input.BloodPressureSummary,
                    ["bloodSugar"] = input.BloodSugarSummary,
                    ["weight"] = input.WeightSummary,
                    ["temperature"] = input.TemperatureSummary,
                    ["bloodOxygen"] = input.BloodOxygenSummary
                }
            };

            var prompt = $"""
You are writing a weekly health dashboard report for a family caregiver displayed on a mobile app.

Use only the facts provided below. Do not invent diagnoses, medical history, or numbers.
All output must be in Traditional Chinese. Tone: warm, empathetic, specific, like a caring nurse speaking to a family member.

## heroReport
- headline: 一句話總結本週健康狀況，15-25 字。例如「本週血壓趨於穩定，血糖仍需留意飯後波動」。
- body: 2-3 句完整段落（80-120 字），必須引用至少兩項具體指標數據與趨勢方向。例如「近七天收縮壓平均為 125 mmHg，較上週下降約 4%，已回到理想區間。血糖在飯後仍有明顯起伏，平均值 138 mg/dL，建議持續觀察。」不要寫空泛的鼓勵話，要有數據支撐。
- tone: supportive / neutral / watchful（依整體狀況選擇）。
- confidence: high / medium / low（依資料充足程度選擇）。

## keyInsight
- label: 固定為「關鍵數據洞察」。
- body: 1-2 句（50-80 字），指出最值得注意的數據變化或模式。必須提到具體指標名稱、數值或百分比變化。例如「血糖飯後平均值較上週上升 12%，集中在週三與週五晚餐後，建議留意這兩天的飲食內容。」
- metricType: 對應的指標代碼，例如 blood_pressure、blood_sugar、weight、temperature、blood_oxygen。如果是綜合觀察用 general。
- severity: low / medium / high。

## actionSuggestion
- label: 固定為「健康行動建議」。
- body: 1-2 句具體可執行的建議（50-80 字），包含飲食、作息或量測習慣的明確行動。例如「建議將晚餐澱粉攝取量減少約 15%，並在飯後 30 分鐘進行 10-15 分鐘的散步，有助於穩定飯後血糖。」不要寫「持續加油」這類空話。
- priority: low / medium / high。
- timeframe: today / this_week。

## todayCards（1-3 張卡片）
- title: 卡片標題，例如「AI 分析摘要」、「今日量測進度」。
- summary: 今日狀態的簡短回饋（30-50 字）。如果今日有量測，給予具體肯定或提醒；如果沒有量測，提醒使用者新增。
- progressNote: 進度說明，例如「今日健康任務達成 60%」。
- iconType: insight / progress / reminder。
- tone: supportive / neutral。

## todaySummary（舊版欄位）
- 給今日卡片用的簡短摘要，30-50 字。

## overallSummary（舊版欄位）
- 與 heroReport.headline 內容一致即可，15-25 字。

## keyInsights（舊版欄位）
- 與 keyInsight.body 的第一句重點一致，25-40 字。

## recommendations（舊版欄位）
- 與 actionSuggestion.body 內容一致即可。

## alerts
- 如果某項指標資料不足或有異常趨勢，產生提醒。沒有需要提醒的情況可以回傳空陣列。
- type: data_gap / reminder / observation。
- message: 提醒內容（20-40 字）。
- severity: low / medium / high。

## trendLabels
- 每項指標給一個 2-4 字的狀態標籤：穩定、正常、需觀察、資料不足。

## Important
- 如果整體資料不足（totalRecordCount < 3），所有內容都要如實反映，不要假裝有足夠資料做分析。
- 不要在任何欄位重複貼上相同的句子。每個欄位的內容應該有不同的重點。

Facts:
{JsonSerializer.Serialize(facts, JsonOptions)}
""";

            return new
            {
                model,
                instructions = "You are a warm, professional caregiver assistant writing for a premium mobile health dashboard. Use Traditional Chinese. Be empathetic and factual. Always reference concrete data when available. Never use vague encouragement without data support.",
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
                                    description = "舊版相容欄位。與 heroReport.headline 內容一致，15-25 字繁體中文。"
                                },
                                todaySummary = new
                                {
                                    type = "string",
                                    description = "舊版相容欄位。今日簡短回饋，30-50 字繁體中文。有量測時給具體肯定，無量測時提醒新增。"
                                },
                                keyInsights = new
                                {
                                    type = "string",
                                    description = "舊版相容欄位。與 keyInsight.body 第一句重點一致，25-40 字繁體中文。"
                                },
                                recommendations = new
                                {
                                    type = "string",
                                    description = "舊版相容欄位。與 actionSuggestion.body 內容一致。"
                                },
                                heroReport = new
                                {
                                    type = "object",
                                    description = "首屏 AI 分析報告，是整份報告最重要的區塊。",
                                    additionalProperties = false,
                                    properties = new
                                    {
                                        headline = new { type = "string", description = "一句話總結本週健康狀況，15-25 字。" },
                                        body = new { type = "string", description = "2-3 句完整段落（80-120 字），必須引用至少兩項具體指標數據與趨勢方向，不要空泛鼓勵。" },
                                        tone = new { type = "string", description = "supportive / neutral / watchful" },
                                        confidence = new { type = "string", description = "high / medium / low，依資料充足程度決定。" }
                                    },
                                    required = new[] { "headline", "body", "tone", "confidence" }
                                },
                                keyInsight = new
                                {
                                    type = "object",
                                    description = "關鍵數據洞察區塊，指出最值得注意的變化。",
                                    additionalProperties = false,
                                    properties = new
                                    {
                                        label = new { type = "string", description = "固定為「關鍵數據洞察」。" },
                                        body = new { type = "string", description = "1-2 句（50-80 字），必須提到具體指標名稱、數值或百分比變化。" },
                                        metricType = new { type = "string", description = "對應指標代碼：blood_pressure / blood_sugar / weight / temperature / blood_oxygen / general。" },
                                        severity = new { type = "string", description = "low / medium / high。" }
                                    },
                                    required = new[] { "label", "body", "metricType", "severity" }
                                },
                                actionSuggestion = new
                                {
                                    type = "object",
                                    description = "健康行動建議區塊，提供具體可執行的下一步。",
                                    additionalProperties = false,
                                    properties = new
                                    {
                                        label = new { type = "string", description = "固定為「健康行動建議」。" },
                                        body = new { type = "string", description = "1-2 句具體建議（50-80 字），包含飲食、作息或量測習慣的明確行動，不要寫空話。" },
                                        priority = new { type = "string", description = "low / medium / high。" },
                                        timeframe = new { type = "string", description = "today / this_week。" }
                                    },
                                    required = new[] { "label", "body", "priority", "timeframe" }
                                },
                                todayCards = new
                                {
                                    type = "array",
                                    description = "今日健康摘要卡片，1-3 張。",
                                    minItems = 1,
                                    maxItems = 3,
                                    items = new
                                    {
                                        type = "object",
                                        additionalProperties = false,
                                        properties = new
                                        {
                                            title = new { type = "string", description = "卡片標題，例如「AI 分析摘要」或「今日量測進度」。" },
                                            summary = new { type = "string", description = "今日狀態回饋，30-50 字。" },
                                            progressNote = new { type = "string", description = "進度說明，例如「今日健康任務達成 60%」。" },
                                            iconType = new { type = "string", description = "insight / progress / reminder。" },
                                            tone = new { type = "string", description = "supportive / neutral。" }
                                        },
                                        required = new[] { "title", "summary", "progressNote", "iconType", "tone" }
                                    }
                                },
                                alerts = new
                                {
                                    type = "array",
                                    description = "提醒或資料缺口警示。沒有需要提醒時回傳空陣列。",
                                    items = new
                                    {
                                        type = "object",
                                        additionalProperties = false,
                                        properties = new
                                        {
                                            type = new { type = "string", description = "data_gap / reminder / observation。" },
                                            message = new { type = "string", description = "提醒內容，20-40 字繁體中文。" },
                                            severity = new { type = "string", description = "low / medium / high。" }
                                        },
                                        required = new[] { "type", "message", "severity" }
                                    }
                                },
                                trendLabels = new
                                {
                                    type = "object",
                                    description = "各健康指標的趨勢狀態標籤，2-4 字。無資料時用「資料不足」。",
                                    additionalProperties = false,
                                    properties = new
                                    {
                                        bloodPressure = new { type = "string", description = "例如：穩定、正常、需觀察、資料不足。" },
                                        bloodOxygen = new { type = "string", description = "例如：穩定、正常、需觀察、資料不足。" },
                                        bloodSugar = new { type = "string", description = "例如：穩定、正常、需觀察、資料不足。" },
                                        temperature = new { type = "string", description = "例如：穩定、正常、需觀察、資料不足。" },
                                        weight = new { type = "string", description = "例如：穩定、正常、需觀察、資料不足。" }
                                    },
                                    required = new[] { "bloodPressure", "bloodOxygen", "bloodSugar", "temperature", "weight" }
                                }
                            },
                            required = new[]
                            {
                                "overallSummary",
                                "todaySummary",
                                "keyInsights",
                                "recommendations",
                                "heroReport",
                                "keyInsight",
                                "actionSuggestion",
                                "todayCards",
                                "alerts",
                                "trendLabels"
                            }
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

        private static T? DeserializeSection<T>(JsonElement root, string propertyName)
        {
            if (!root.TryGetProperty(propertyName, out var section) || section.ValueKind == JsonValueKind.Null)
            {
                return default;
            }

            return JsonSerializer.Deserialize<T>(section.GetRawText(), JsonOptions);
        }

        private static List<T> DeserializeList<T>(JsonElement root, string propertyName)
        {
            if (!root.TryGetProperty(propertyName, out var section) || section.ValueKind != JsonValueKind.Array)
            {
                return new List<T>();
            }

            return JsonSerializer.Deserialize<List<T>>(section.GetRawText(), JsonOptions) ?? new List<T>();
        }

        private static string FirstNonEmpty(params string?[] candidates)
        {
            return candidates.FirstOrDefault(x => !string.IsNullOrWhiteSpace(x))?.Trim() ?? string.Empty;
        }

        private static string ComputeSourceDataHash(HealthInsightPromptInput input)
        {
            var serialized = JsonSerializer.Serialize(input, JsonOptions);
            var bytes = Encoding.UTF8.GetBytes(serialized);
            var hash = SHA256.HashData(bytes);
            return Convert.ToHexString(hash);
        }
    }
}
