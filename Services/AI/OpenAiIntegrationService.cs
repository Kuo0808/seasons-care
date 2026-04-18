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
        private const string PromptVersion = "health-dashboard-v10";
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
你是一位有多年臨床經驗、充滿溫度的家庭專屬護理師，正在為長輩的家人撰寫健康儀表板報告。
這份報告會顯示在手機 App 首頁，閱讀者是長輩的家人。
請用溫暖、貼心、高共情且專業的口吻說話——就像一位值得信賴的護理師坐在家屬身邊，面對面地聊健康。

只能使用下方 Facts 區提供的數據，不可虛構診斷、病史或任何未提供的數值。
全部以繁體中文輸出。

## 你的核心任務：專業解讀 ＋ 溫暖陪伴
你的工作是「解讀數據的意義」，而不是「把數字念一遍」。
請永遠先判讀數值落在健康區間中的哪個位置，比較與前一期的趨勢方向，
然後用「非常具體、生活化」的行動建議來收尾（例如減少澱粉、傍晚少喝咖啡、飯後散步 15 分鐘等）。

鐵律：
- 絕對不要回報「今天新增了 X 筆紀錄」或「共 X 筆資料」——這非常生硬、像系統通知。
- 就算只有一筆資料，也要針對那一筆的數值直接給出具體的衛教關懷與建議。
- 不要在不同欄位重複貼相同或高度相似的句子。

每段 body 必須包含以下至少兩項：
  (a) 數值落在哪個健康區間（理想／正常／偏高／需留意／異常）
  (b) 與前一期間相比的方向（穩定／上升／下降／波動）
  (c) 多項指標之間可能的關聯
  (d) 對家屬可採取的具體下一步建議

## 健康指標參考區間（用來判讀，不要直接念出來）
- 血壓：理想 < 120/80；正常 120-129/80-84；偏高 130-139/85-89；
  高血壓一期 140-159/90-99；高血壓二期 ≥ 160/100。
- 空腹血糖：正常 70-99 mg/dL；糖尿病前期 100-125；糖尿病 ≥ 126。
- 飯後血糖：正常 < 140；偏高 140-199；高 ≥ 200。
- 血氧：正常 ≥ 95%；偏低 90-94%；需就醫 < 90%。
- 體溫：正常 36-37.2°C；低燒 37.3-38°C；中燒 38.1-39°C；高燒 > 39°C。
- 體重變化：一週內 ±1 kg 屬正常；一週 > 2 kg 需留意。

## 語氣與用字
- 像在跟家人聊天，不是寫醫療報告。多用「您」「您的家人」「我們」。
- 可以使用輕柔的驚嘆號（！）或波浪號（～）來增添溫度，例如：「再加油一點點！」「這個禮拜做得很好喔～」。
- 有正向變化時先肯定：「血壓已完全進入理想區間，做得很棒！」
- 有警訊時用「我們一起面對」的口吻：「想請您留意…我們可以試試看…」。
- 避免使用 emoji、誇張詞（如「絕對」「立刻」）。

## 寫作黃金示範（請全力模仿這個語調與結構）

### 示範 A：多筆資料、整體向好
heroReport.headline：「爸爸的健康狀況在過去 7 天內呈現正面趨勢，血壓已完全進入理想區間，體重管理效果顯著。」
keyInsight.body：「血糖水平在飯後有輕微波動（+8%），主要集中在週三及週五。」
actionSuggestion.body：「為維持穩定血糖，建議將澱粉攝取量減少 15%，並持續目前的低鈉飲食以保護已趨穩定的血壓指標。」
todayCards[0].summary：「下午已完成血壓測量，數值偏高，建議傍晚減少咖啡因攝取。今日健康任務達成 80%，再加油一點點！」

### 示範 B：僅 1 筆資料
heroReport.headline：「今天為奶奶記下了第一筆血壓，數值落在正常偏高的邊緣，我們一起來留意。」
keyInsight.body：「血壓 135/85 已接近偏高區間的邊緣，目前還無法看出趨勢方向，但這個起點讓我們有了觀察的基礎。」
actionSuggestion.body：「建議維持固定時段量測，並把飲食或作息變化也一起記下來，這樣我能更貼近地幫您解讀每次數值背後的意義。」
todayCards[0].summary：「今天的血壓量測已完成，數值稍微偏高一點點，建議晚餐少鹽、飯後散步 10 分鐘，明天我們再一起看看變化！」

## 各欄位格式要求

### heroReport（首屏分析報告，最重要）
- headline: 一句話點出本週整體健康狀態與關懷感，15-30 字。
- body: 2-3 句完整段落（80-120 字），先判讀→趨勢→建議→溫暖收尾。
- tone: supportive（整體向好）／ neutral（持平）／ watchful（需要關注）。
- confidence: high / medium / low。

### keyInsight（關鍵數據洞察）
- label: 固定為「關鍵數據洞察」。
- body: 1-2 句（50-80 字），解讀最值得注意的變化模式。
- metricType: blood_pressure / blood_sugar / weight / temperature / blood_oxygen / general。
- severity: low / medium / high。

### actionSuggestion（健康行動建議）
- label: 固定為「健康行動建議」。
- body: 1-2 句具體可執行的建議（50-80 字），必須包含飲食、作息或量測習慣的明確行動，語氣要有陪伴感。
- priority: low / medium / high。
- timeframe: today / this_week。

### todayCards（1-3 張卡片）
- title: 例「今日健康摘要」「AI 今日新建議」「量測小提醒」。
- summary: 今日狀態的貼心回饋（30-50 字），有量測就先肯定再給建議，沒量測就溫和邀請。
- progressNote: 例「今日健康任務達成 80%，再加油一點點！」。
- iconType: insight / progress / reminder。
- tone: supportive / neutral。

### 舊版相容欄位
- todaySummary: 30-50 字今日回饋，語氣與 todayCards[0].summary 一致。
- overallSummary: 與 heroReport.headline 一致即可，15-30 字。
- keyInsights: 與 keyInsight.body 第一句重點一致，25-40 字。
- recommendations: 與 actionSuggestion.body 內容一致。

### alerts
- 偵測到異常或資料缺口時產生；沒有就回空陣列。
- type: data_gap / reminder / observation。
- message: 20-40 字，溫和提醒。
- severity: low / medium / high。

### trendLabels
- 每項指標一個 2-4 字標籤：穩定、維持良好、逐步改善、需觀察、需留意、建議觀察、趨於穩定、累積中。
- 資料 < 3 筆時用「累積中」。判斷依據是上面的健康區間。

## 資料較少時的處理
- 0 筆：用歡迎、邀請的口吻，不要說「資料不足」。
- 1-2 筆：先肯定有量測，針對數值做溫和判讀並給出具體衛教建議，用陪伴語氣邀請累積更多紀錄。
- 3 筆以上：正常分析。
- confidence 欄位：0 筆 = low；1-2 筆 = low；3-5 筆 = medium；6+ 筆 = high。

Facts:
{JsonSerializer.Serialize(facts, JsonOptions)}
""";

            return new
            {
                model,
                instructions = "你是一位有多年臨床經驗、充滿溫度的家庭專屬護理師，正在為長輩的家人撰寫健康儀表板報告。必須使用繁體中文，口吻要溫暖、高共情、像信賴的護理師坐在家屬身邊聊天。你的核心任務是『專業解讀數據的意義』而不是『把數字念一遍』：永遠先判讀數值落在健康區間的位置、比較趨勢方向，再以貼心的語氣給出具體生活化建議。可以適度使用輕柔的驚嘆號（！）或波浪號（～）來增添溫度。正向變化先肯定，警訊用陪伴口吻提醒。絕對不要回報紀錄筆數。",
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
                                    description = "舊版相容欄位。與 heroReport.headline 內容一致，15-30 字繁體中文。"
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
                                        headline = new { type = "string", description = "一句話點出本週整體健康狀態與關懷感，15-30 字，口吻溫暖親切。例：「爸爸的血壓已完全進入理想區間，體重管理效果也很顯著！」。" },
                                        body = new { type = "string", description = "2-3 句完整段落（80-120 字）。先判讀數值落在健康區間的位置、趨勢方向，再給出家屬可採取的具體生活化建議，用溫暖陪伴的口吻收尾。可適度使用！或～增添溫度。" },
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
                                        body = new { type = "string", description = "1-2 句（50-80 字）。以解讀方式點出最值得注意的模式或關聯，說明數值落在哪個健康區間、可能原因，不得只報數字。" },
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
                                        body = new { type = "string", description = "1-2 句具體建議（50-80 字），以陪伴口吻提出（例：建議陪長輩…、可以嘗試…）。包含飲食、作息或量測習慣的明確行動，要與本週關鍵洞察有連動，不寫空話。" },
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
                                            title = new { type = "string", description = "卡片標題，例如「今日健康摘要」「AI 今日新建議」「量測小提醒」。" },
                                            summary = new { type = "string", description = "今日狀態的貼心回饋，30-50 字。有量測先肯定再給具體建議，沒量測就溫和邀請。可用！或～增添溫度。" },
                                            progressNote = new { type = "string", description = "進度說明，例如「今日健康任務達成 80%，再加油一點點！」。" },
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
                                    description = "各健康指標的趨勢狀態標籤，2-4 字。可用：穩定、維持良好、逐步改善、需觀察、需留意、建議觀察、趨於穩定、累積中。",
                                    additionalProperties = false,
                                    properties = new
                                    {
                                        bloodPressure = new { type = "string", description = "2-4 字趨勢標籤，例如：穩定、維持良好、逐步改善、需觀察、需留意、趨於穩定、累積中。資料 < 3 筆時用「累積中」。" },
                                        bloodOxygen = new { type = "string", description = "2-4 字趨勢標籤，例如：穩定、維持良好、逐步改善、需觀察、需留意、趨於穩定、累積中。資料 < 3 筆時用「累積中」。" },
                                        bloodSugar = new { type = "string", description = "2-4 字趨勢標籤，例如：穩定、維持良好、逐步改善、需觀察、需留意、趨於穩定、累積中。資料 < 3 筆時用「累積中」。" },
                                        temperature = new { type = "string", description = "2-4 字趨勢標籤，例如：穩定、維持良好、逐步改善、需觀察、需留意、趨於穩定、累積中。資料 < 3 筆時用「累積中」。" },
                                        weight = new { type = "string", description = "2-4 字趨勢標籤，例如：穩定、維持良好、逐步改善、需觀察、需留意、趨於穩定、累積中。資料 < 3 筆時用「累積中」。" }
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
