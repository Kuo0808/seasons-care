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
        private const string PromptVersion = "health-dashboard-v13";
        private const int MaxRetryAttempts = 3;
        private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
        private static readonly string[] TrendLabelEnum = { "維持良好", "逐步改善", "趨於穩定", "建議觀察", "累積中" };

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
            var model = string.IsNullOrWhiteSpace(_options.Value.Model) ? "gpt-4.1" : _options.Value.Model;

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
                GetOptionalString(root, "overallSummary"),
                heroReport.Headline,
                heroReport.Body);
            var legacyTodaySummary = FirstNonEmpty(
                GetOptionalString(root, "todaySummary"),
                todayCards.FirstOrDefault()?.Summary,
                heroReport.Body);
            var legacyKeyInsights = FirstNonEmpty(
                GetOptionalString(root, "keyInsights"),
                keyInsightSection.Body,
                heroReport.Body);
            var legacyRecommendations = FirstNonEmpty(
                GetOptionalString(root, "recommendations"),
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
                todaySummary = input.TodaySummary,
                clinicalSummary = input.ClinicalSummary,
                narrativeDirective = input.NarrativeDirective,
                fewShotScenarios = input.FewShotScenarios,
                priorityFindings = input.PriorityFindings,
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
你是一位有臨床判讀能力的家庭照護助理，正在為長輩家屬撰寫 7 天健康儀表板報告。
請全部使用繁體中文，語氣溫暖、具體、像家庭護理師在對家人說話。

你現在的第一優先不是報數字，而是根據 Facts 中已整理好的 priorityFindings 做判讀。
如果 priorityFindings 有內容：
- heroReport 必須先講第一個 finding
- keyInsight 必須聚焦最重要的異常或變化
- actionSuggestion 必須對應同一個 finding，給出下一步
- 不要把紀錄筆數當主句

如果 priorityFindings 沒有高風險異常，才改寫成穩定、持續觀察、逐步累積的敘事。

只能使用下方 Facts 區提供的資料，不可虛構診斷、病史或額外數值。

參考區間：
- 血壓：理想 < 120/80；正常 120-129/80-84；偏高 130-139/85-89；高血壓一期 140-159/90-99；高血壓二期 >= 160/100
- 空腹血糖：正常 70-99 mg/dL；糖尿病前期 100-125；糖尿病 >= 126
- 飯後血糖：正常 < 140；偏高 140-199；高 >= 200
- 血氧：正常 >= 95%；偏低 90-94%；需就醫 < 90%
- 體溫：正常 36-37.2°C；低燒 37.3-38°C；中燒 38.1-39°C；高燒 > 39°C
- 體重變化：一週內 ±1 kg 屬正常；一週 > 2 kg 需留意

嚴格規則：
- 所有字數限制都是「硬性上限」，超過會被裁切，請務必精簡
- 禁止把「近 7 天有幾筆紀錄」當成 body 的主體
- 禁止只重述數值或平均值
- 禁止在文字中出現英文欄位名（如 before_meal、systolic 等）
- 有異常時先講異常，再補溫和建議
- 有正向變化時先肯定，再補維持方式
- 當資料少時，不要說「資料不足」，改用「累積中」或更溫和的說法

few-shot 風格示範（請嚴格遵守字數）：

示範 A：多指標異常
- heroReport.headline: 本週血壓與飯後血糖偏高
- heroReport.body: 王爸爸這週血壓與飯後血糖偏高，建議調整晚餐並留意作息。
- keyInsight.body: 血壓與飯後血糖同步偏高，需留意。
- actionSuggestion.body: 晚餐澱粉減量、飯後散步 10 分鐘，並固定時段量測。
- todayCards[0].summary: 今天血壓略偏高，建議飯後散步 10 分鐘，達成 60%。

示範 B：穩定維持中
- heroReport.headline: 本週數據穩定，維持得很好
- heroReport.body: 王爸爸這週血壓進入理想區間，體重管理效果也很顯著。
- keyInsight.body: 血壓與體重持平，整體穩定。
- actionSuggestion.body: 維持現有飲食與運動節奏，持續定時量測即可。
- todayCards[0].summary: 今日數據穩定，繼續維持飲食與量測習慣。

trendLabels 評語選擇規則（每個指標只能從以下五選一）：
- 「維持良好」：數據在理想區間且穩定
- 「逐步改善」：趨勢正在往好的方向走
- 「趨於穩定」：略偏離區間但持平、無惡化
- 「建議觀察」：數據偏離區間或波動偏大
- 「累積中」：資料筆數太少無法判讀

輸出要求（字數為硬性上限，請精簡）：
- overallSummary：15-25 字，與 heroReport.headline 同方向
- todaySummary：30-40 字，與 todayCards[0].summary 同方向
- keyInsights：15-20 字，對應 keyInsight.body 第一重點
- recommendations：25-30 字，對應 actionSuggestion.body

- heroReport.headline：12-18 字，一句話講本週最重要的判讀
- heroReport.body：25-35 字，精簡敘述本週重點與下一步
- heroReport.tone：supportive / neutral / watchful
- heroReport.confidence：high / medium / low

- keyInsight.label：固定為「關鍵數據洞察」
- keyInsight.body：15-20 字，聚焦第一優先 finding，不放數值細節
- keyInsight.metricType：blood_pressure / blood_sugar / weight / temperature / blood_oxygen / general
- keyInsight.severity：low / medium / high

- actionSuggestion.label：固定為「健康行動建議」
- actionSuggestion.body：25-30 字，必須對應 keyInsight，給出可執行動作
- actionSuggestion.priority：low / medium / high
- actionSuggestion.timeframe：today / this_week

- todayCards：1-3 張
- todayCards[*].summary：30-40 字，口語化、避免列出原始數據與英文欄位名
- alerts：沒有提醒就回空陣列
- trendLabels：每個指標只能填上方五個評語其中一個

Facts:
{JsonSerializer.Serialize(facts, JsonOptions)}
""";

            return new
            {
                model,
                instructions = "你是具臨床判讀能力的家庭照護助理。你必須優先根據 priorityFindings 進行分析，不可只複述 metrics，也不可把紀錄筆數當主句。請使用繁體中文，口吻溫暖、具體、可信。",
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
                                    description = "舊版相容欄位。與 heroReport.headline 同方向，15-30 字繁體中文。"
                                },
                                todaySummary = new
                                {
                                    type = "string",
                                    description = "舊版相容欄位。今日簡短回饋，30-50 字繁體中文。"
                                },
                                keyInsights = new
                                {
                                    type = "string",
                                    description = "舊版相容欄位。對應 keyInsight.body 第一重點，25-40 字。"
                                },
                                recommendations = new
                                {
                                    type = "string",
                                    description = "舊版相容欄位。與 actionSuggestion.body 保持一致。"
                                },
                                heroReport = new
                                {
                                    type = "object",
                                    additionalProperties = false,
                                    properties = new
                                    {
                                        headline = new { type = "string" },
                                        body = new { type = "string" },
                                        tone = new { type = "string" },
                                        confidence = new { type = "string" }
                                    },
                                    required = new[] { "headline", "body", "tone", "confidence" }
                                },
                                keyInsight = new
                                {
                                    type = "object",
                                    additionalProperties = false,
                                    properties = new
                                    {
                                        label = new { type = "string" },
                                        body = new { type = "string" },
                                        metricType = new { type = "string" },
                                        severity = new { type = "string" }
                                    },
                                    required = new[] { "label", "body", "metricType", "severity" }
                                },
                                actionSuggestion = new
                                {
                                    type = "object",
                                    additionalProperties = false,
                                    properties = new
                                    {
                                        label = new { type = "string" },
                                        body = new { type = "string" },
                                        priority = new { type = "string" },
                                        timeframe = new { type = "string" }
                                    },
                                    required = new[] { "label", "body", "priority", "timeframe" }
                                },
                                todayCards = new
                                {
                                    type = "array",
                                    minItems = 1,
                                    maxItems = 3,
                                    items = new
                                    {
                                        type = "object",
                                        additionalProperties = false,
                                        properties = new
                                        {
                                            title = new { type = "string" },
                                            summary = new { type = "string" },
                                            progressNote = new { type = "string" },
                                            iconType = new { type = "string" },
                                            tone = new { type = "string" }
                                        },
                                        required = new[] { "title", "summary", "progressNote", "iconType", "tone" }
                                    }
                                },
                                alerts = new
                                {
                                    type = "array",
                                    items = new
                                    {
                                        type = "object",
                                        additionalProperties = false,
                                        properties = new
                                        {
                                            type = new { type = "string" },
                                            message = new { type = "string" },
                                            severity = new { type = "string" }
                                        },
                                        required = new[] { "type", "message", "severity" }
                                    }
                                },
                                trendLabels = new
                                {
                                    type = "object",
                                    additionalProperties = false,
                                    properties = new
                                    {
                                        bloodPressure = new { type = "string", @enum = TrendLabelEnum, description = "血壓評語，只能從五個固定詞彙擇一" },
                                        bloodOxygen = new { type = "string", @enum = TrendLabelEnum, description = "血氧評語，只能從五個固定詞彙擇一" },
                                        bloodSugar = new { type = "string", @enum = TrendLabelEnum, description = "血糖評語，只能從五個固定詞彙擇一" },
                                        temperature = new { type = "string", @enum = TrendLabelEnum, description = "體溫評語，只能從五個固定詞彙擇一" },
                                        weight = new { type = "string", @enum = TrendLabelEnum, description = "體重評語，只能從五個固定詞彙擇一" }
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

        private static string GetOptionalString(JsonElement root, string propertyName)
        {
            return root.TryGetProperty(propertyName, out var property) && property.ValueKind == JsonValueKind.String
                ? property.GetString() ?? string.Empty
                : string.Empty;
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
