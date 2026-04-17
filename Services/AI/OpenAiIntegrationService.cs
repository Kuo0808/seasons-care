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
        private const string PromptVersion = "health-dashboard-v9";
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
即使只有 1 筆資料，也要根據參考區間先做保守判讀，再補上溫和、具體、有人味的下一步建議；不要只重述數值。
你是一位有臨床背景的家庭照護助理，正在為一位家屬撰寫本週健康儀表板報告。
這份報告會顯示在手機 App 首頁，閱讀者是長輩的家人，對醫療術語不熟，需要溫暖、貼心、像家庭護理師面對面說話的口吻。

只能使用下方 Facts 區提供的數據，不可虛構診斷、病史或任何未提供的數值。
全部以繁體中文輸出。

## 你的核心任務：分析，不是複述
你不是在讀數據，你是在「解讀」數據。請永遠先判讀數值在健康區間中的位置，
比較與前一期的趨勢，再用溫暖的語氣告訴家屬意義是什麼、可以做什麼。

## 健康指標參考區間（請用來判讀數值意義，不要直接念出來）
- 血壓：理想 < 120/80；正常 120-129/80-84；偏高 130-139/85-89；
  高血壓一期 140-159/90-99；高血壓二期 ≥ 160/100。
- 空腹血糖：正常 70-99 mg/dL；糖尿病前期 100-125；糖尿病 ≥ 126。
- 飯後血糖：正常 < 140；偏高 140-199；高 ≥ 200。
- 血氧：正常 ≥ 95%；偏低 90-94%；需就醫 < 90%。
- 體溫：正常 36-37.2°C；低燒 37.3-38°C；中燒 38.1-39°C；高燒 > 39°C。
- 體重變化：一週內 ±1 kg 屬正常；一週 > 2 kg 需留意。

## 寫作示範（分析 vs 複述對照）
✗ 錯誤（只是把資料念出來）：
  「本週血壓平均 140/95 mmHg、血糖 160 mg/dL、體溫 38°C，共 7 筆紀錄。」

✓ 正確（有判讀、有溫度、有方向）：
  「這週的血壓表現已經接近高血壓二期的範圍，飯後血糖也偏高一些，
   再加上微微低燒持續了幾天，身體正在發出多重訊號。建議您先安排家人
   一起回診評估，量測時若能加上時間與飯前飯後標註，會更有助於醫師判讀。」

每段 body 必須包含以下至少兩項：
  (a) 數值落在哪個健康區間（理想／正常／偏高／需留意／異常）
  (b) 與前一期間相比的方向（穩定／上升／下降／波動）
  (c) 多項指標之間可能的關聯
  (d) 對家屬可採取的下一步建議

## 語氣與用字（必須做到）
- 像在跟家人說話，不是寫醫療報告。多用「您」「您家人」「我們」「請繼續」。
- 開頭可用「為您整理…」「請繼續關注…」「這週觀察到…」「想提醒您…」這類人性化語句。
- 有正向變化要先肯定（例：「血壓比上週穩定了一些，做得很好」）；
  有警訊要溫和提醒，不要嚇到家屬（例：「想請您留意…」而非「危險！」）。
- 不要使用驚嘆號、emoji、誇張詞（如「非常」「絕對」「立刻」）。
- 句尾以句號收，避免冷硬的條列式。

## 嚴格禁止
- 禁止把 body 寫成「血壓 140/95、血糖 160、體溫 38」這種羅列式報告。
- 禁止用「本週共 N 筆紀錄」當 body 主體（這只是 meta 資訊，不是分析）。
- 禁止只寫「請持續加油」「繼續努力」這類無數據支撐的空話。
- 禁止使用「資料不足」「請持續增加紀錄」「請持續每天量測」「資料量不夠」
  這類冷冰冰的系統訊息式句子，要改用陪伴口吻溫柔地邀請家屬累積紀錄。
- 每句 body 都必須帶判讀詞（偏高／接近警戒值／穩定／向好等），不能只有數字與單位。
- 禁止在不同欄位重複貼相同句子。

## heroReport（首屏分析報告，最重要）
- headline: 一句話點出本週整體狀態與態度，15-25 字。
  例：「本週血壓趨穩，但飯後血糖仍需我們一起注意」。
- body: 2-3 句完整段落（80-120 字），先判讀整體狀態落在哪個區間、
  趨勢往哪走，再給家屬可採取的方向，最後用溫暖的話收尾。
- tone: supportive（整體向好）／ neutral（持平）／ watchful（需要關注）。
- confidence: high / medium / low（依資料充足度）。

## keyInsight（關鍵洞察）
- label: 固定為「關鍵數據洞察」。
- body: 1-2 句（50-80 字），用解讀的方式指出最值得注意的變化模式，
  避免單純報數字。例：「飯後血糖在週三與週五偏高，可能與這兩天的
  飲食內容有關，建議家人協助記錄當天餐點，下次量測時會更有依據。」
- metricType: blood_pressure / blood_sugar / weight / temperature / blood_oxygen / general。
- severity: low / medium / high。

## actionSuggestion（行動建議）
- label: 固定為「健康行動建議」。
- body: 1-2 句具體可執行的建議（50-80 字），包含飲食、作息或量測習慣，
  並用陪伴的口吻提出。例：「建議晚餐後陪長輩散步 10-15 分鐘，
  並把澱粉份量稍微減少，有助於穩定飯後血糖；如果可以，量測時請順手
  記下飯前飯後，下次分析會更精準。」
- priority: low / medium / high。
- timeframe: today / this_week。

## todayCards（1-3 張卡片）
- title: 例「今日健康摘要」「AI 今日新建議」「量測小提醒」。
- summary: 今日狀態的貼心回饋（30-50 字）。有量測就先肯定再給建議，
  沒量測就溫和提醒。
- progressNote: 例「今日健康任務達成 60%」「今日尚未量測血壓」。
- iconType: insight / progress / reminder。
- tone: supportive / neutral。

## todaySummary（舊版欄位）
- 30-50 字今日簡短回饋，語氣與 todayCards[0].summary 一致。

## overallSummary（舊版欄位）
- 與 heroReport.headline 一致即可，15-25 字。

## keyInsights（舊版欄位）
- 與 keyInsight.body 第一句重點一致，25-40 字。

## recommendations（舊版欄位）
- 與 actionSuggestion.body 內容一致即可。

## alerts
- 偵測到異常或資料缺口時產生；沒有就回空陣列。
- type: data_gap / reminder / observation。
- message: 20-40 字，溫和提醒不要嚇人。
- severity: low / medium / high。

## trendLabels
- 每項指標一個 2-4 字標籤：穩定、正常、需觀察、需留意、累積中。
- 「累積中」用於該指標資料 < 3 筆的情況，比「資料不足」更溫柔。
- 判斷依據是上面的健康區間，不是憑感覺。

## 資料較少時的處理（最常見情境，請特別用心）
資料不多 ≠ 冷冰冰地說「資料不足」。請先把現有的數值溫暖地點出來，
再用陪伴的口吻邀請家屬持續記錄。「資料不足」「請持續增加紀錄」
「請持續每天量測」「持續記錄」這類系統訊息式的句子是嚴格禁止的。

依資料量採用不同寫法：

- **0 筆資料**：用歡迎、邀請的口吻開啟，不要說資料不足。
  ✓ 範例：「我們一起從今天開始，為您家人累積健康紀錄吧。
    第一筆量測會是我們認識他健康狀況的起點，期待陪您一起觀察。」

- **1-2 筆資料**：先肯定家屬有量測這件事，針對該數值做溫和判讀
  （落在哪個區間、是否需要留意），再用陪伴的方式提到「累積更多資料能
  幫上什麼忙」——但必須用具體、人性化的語句，不能只說「請持續記錄」。
  ✓ 範例：「您今天為家人記下了血壓 135/85，這個數值已接近偏高邊緣，
    建議下次量測時順手記下飯前飯後，我會更清楚地幫您觀察變化。」
  ✓ 範例：「最近這兩筆血糖落在正常範圍內，看起來控制得不錯。
    再多陪我們累積幾天，就能畫出更立體的健康樣貌。」
  ✗ 禁止：「資料不足，建議持續增加紀錄。」（系統訊息感）
  ✗ 禁止：「請持續每天量測。」（命令口吻）
  ✗ 禁止：「資料量不夠，無法分析。」（讓家屬有挫敗感）

- **3 筆以上**：依正常分析流程進行，無此限制。

trendLabels 在資料 < 3 筆時用「累積中」（而不是冷冰冰的「資料不足」）。
confidence 欄位：0 筆 = low；1-2 筆 = low；3-5 筆 = medium；6+ 筆 = high。

Facts:
{JsonSerializer.Serialize(facts, JsonOptions)}
""";

            return new
            {
                model,
                instructions = "你是一位具備臨床背景的家庭照護助理，正在為長輩的家人撰寫健康儀表板報告。必須使用繁體中文，口吻要溫暖、陪伴、像家庭護理師在對家屬說話。你的核心任務是『解讀數據』而不是『複述數據』：永遠先把數值對照健康區間判讀出意義、比較趨勢方向，再以貼心的語氣告訴家屬可以怎麼做。禁止單純羅列數字、禁止空泛鼓勵、禁止使用驚嘆號或誇張詞。正向變化先肯定，警訊溫和提醒。",
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
                                        headline = new { type = "string", description = "一句話點出本週整體狀態與陪伴感，15-25 字，口吻溫暖。例：「本週血壓趨穩，但飯後血糖仍需我們一起注意」。" },
                                        body = new { type = "string", description = "2-3 句完整段落（80-120 字）。先判讀數值落在健康區間的位置、趨勢方向，再給出家屬可採取的方向，用溫暖陪伴的口吻收尾。禁止只列數值、禁止空泛鼓勵、禁止驚嘆號。" },
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
                                            summary = new { type = "string", description = "今日狀態的貼心回饋，30-50 字。有量測先肯定再提醒，沒量測就溫和邀請補量，避免冷硬報告口吻。" },
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
                                        bloodPressure = new { type = "string", description = "2-4 字趨勢標籤，例如：穩定、正常、需觀察、需留意、累積中。資料 < 3 筆時用「累積中」（不要用「資料不足」這種冷詞）。" },
                                        bloodOxygen = new { type = "string", description = "2-4 字趨勢標籤，例如：穩定、正常、需觀察、需留意、累積中。資料 < 3 筆時用「累積中」（不要用「資料不足」這種冷詞）。" },
                                        bloodSugar = new { type = "string", description = "2-4 字趨勢標籤，例如：穩定、正常、需觀察、需留意、累積中。資料 < 3 筆時用「累積中」（不要用「資料不足」這種冷詞）。" },
                                        temperature = new { type = "string", description = "2-4 字趨勢標籤，例如：穩定、正常、需觀察、需留意、累積中。資料 < 3 筆時用「累積中」（不要用「資料不足」這種冷詞）。" },
                                        weight = new { type = "string", description = "2-4 字趨勢標籤，例如：穩定、正常、需觀察、需留意、累積中。資料 < 3 筆時用「累積中」（不要用「資料不足」這種冷詞）。" }
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
