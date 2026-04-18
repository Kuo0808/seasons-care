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
        private const string PromptVersion = "health-dashboard-v12";
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
- 禁止把「近 7 天有幾筆紀錄」當成 body 的主體
- 禁止只重述數值或平均值
- 每段 body 至少要包含：區間判讀、趨勢或關聯、下一步建議 其中兩項
- 有異常時先講異常，再補溫和建議
- 有正向變化時先肯定，再補維持方式
- 當資料少時，不要說「資料不足」，改用「累積中」或更溫和的說法

few-shot 風格示範：

示範 A：多指標異常
- heroReport.headline: 這週血壓與飯後血糖都偏高，建議我們一起多留意
- heroReport.body: 這週最值得注意的是血壓已落在偏高區間，飯後血糖也有偏高情況，表示身體代謝與循環都在提醒我們要更留心。建議先從晚餐內容、飯後活動和固定時段量測開始整理，若接下來幾天仍維持偏高，再安排回診討論會更安心。
- keyInsight.body: 本週的血壓與飯後血糖同時偏高，不像單一數值波動，更適合先觀察飲食與作息是否一起影響。
- actionSuggestion.body: 建議這幾天把晚餐澱粉份量稍微減少，飯後陪家人散步 10 到 15 分鐘，並在相近時段補量血壓與血糖，會更容易看出變化。

示範 B：只有 1 筆但可判讀
- heroReport.headline: 今天先有一筆血壓紀錄，我們已經能看出一些方向
- heroReport.body: 今天這筆血壓 135/85 mmHg 已接近偏高區間，先不用太緊張，但值得我們多留意一下。建議下次量測時維持相近時段並在休息後再量一次，累積幾天後會更容易看出是不是穩定偏高。
- keyInsight.body: 這筆血壓已比理想區間高一些，雖然還不是趨勢，但足以提醒我們先留意休息與量測時機。
- actionSuggestion.body: 建議今天稍晚休息 10 到 15 分鐘後再量一次，並順手記下量測時間，之後就能更清楚比較變化。

輸出要求：
- overallSummary：15-30 字，與 heroReport.headline 同方向
- todaySummary：30-50 字，與 todayCards[0].summary 同方向
- keyInsights：25-40 字，對應 keyInsight.body 的第一重點
- recommendations：對應 actionSuggestion.body

- heroReport.headline：15-30 字，一句話講本週最重要的判讀
- heroReport.body：80-120 字，先說判讀，再說趨勢或關聯，最後給下一步
- heroReport.tone：supportive / neutral / watchful
- heroReport.confidence：high / medium / low

- keyInsight.label：固定為「關鍵數據洞察」
- keyInsight.body：50-80 字，聚焦第一優先 finding
- keyInsight.metricType：blood_pressure / blood_sugar / weight / temperature / blood_oxygen / general
- keyInsight.severity：low / medium / high

- actionSuggestion.label：固定為「健康行動建議」
- actionSuggestion.body：50-80 字，必須對應 keyInsight 的 finding
- actionSuggestion.priority：low / medium / high
- actionSuggestion.timeframe：today / this_week

- todayCards：1-3 張
- alerts：沒有提醒就回空陣列
- trendLabels：2-4 字標籤，資料少時用「累積中」

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
                                        bloodPressure = new { type = "string" },
                                        bloodOxygen = new { type = "string" },
                                        bloodSugar = new { type = "string" },
                                        temperature = new { type = "string" },
                                        weight = new { type = "string" }
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
