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
        private const string PromptVersion = "health-dashboard-v18";
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

參考區間（依據衛福部國民健康署、疾管署及相關學會公告之臨床標準；輸出時不需主動引用來源條文，但若家屬詢問可簡述「依衛福部公告」）：

- 血壓【依據：台灣高血壓學會 2022 高血壓臨床治療指引（居家血壓量測標準）、衛福部國民健康署 2023 成人高血壓防治手冊】
  - 本系統量的是居家血壓，採居家標準（較診間 140/90 略低）
  - 理想 < 120/80；正常 120-134/80-84；居家高血壓門檻 ≥ 135/85（偏高）；明顯偏高 145-159/90-99；高血壓二期 ≥ 160/100
  - 【危險 critical】收縮 ≥ 180 或 舒張 ≥ 110 屬高血壓急症（hypertensive crisis）；本系統為保守警示（台灣高血壓學會 2022 公告之急症門檻為 180/120，本系統提前在 180/110 警示以爭取就醫時間），需立即就醫或撥打 119
  - 【危險 critical】收縮 < 90 屬低血壓休克風險，需立即就醫

- 空腹血糖【依據：衛福部國民健康署糖尿病健康管理手冊、台灣糖尿病學會 2022 第 2 型糖尿病臨床照護指引】
  - 正常 70-99 mg/dL；糖尿病前期（空腹血糖異常 IFG）100-125；糖尿病 ≥ 126
  - 【危險 critical】≥ 250 或 < 54 需立即就醫

- 飯後血糖（餐後 2 小時）【同上來源】
  - 正常 < 140 mg/dL；糖尿病前期（葡萄糖耐受性異常 IGT）140-199；糖尿病 ≥ 200
  - 【危險 critical】≥ 250 需立即就醫；【危險 critical】< 54 屬嚴重低血糖，需立即補糖並就醫

- 血氧【依據：衛福部疾管署 COVID-19 居家照護指引、台灣胸腔暨重症加護醫學會建議】
  - 正常 ≥ 95%；略低 94%；明顯偏低 90-93%（疾管署建議 ≤ 94% 通報就醫評估）
  - 【危險 critical】< 90% 屬嚴重缺氧急症，必須直接建議立即就醫或撥打 119

- 體溫【依據：衛福部疾管署「發燒之臨床診治」、台灣感染症醫學會建議；本系統量的是中心體溫（口溫／耳溫／肛溫）】
  - 正常 36-37.4°C；微燒 37.5-37.9°C；發燒（疾管署官方門檻）≥ 38°C；中燒 38-38.9°C
  - 【危險 critical】≥ 39°C 屬高燒，常伴隨倦怠、肌肉痠痛、意識混亂、脫水等發燒症狀，需立即就醫評估
  - 【危險 critical】≤ 35°C 為失溫急症（hypothermia），需保暖並立即就醫

- 體重變化【依據：衛福部國民健康署成人肥胖防治實證指引】
  - 一週內 ±1 kg 屬正常波動；一週 > 2 kg 或一個月 > 5% 體重需留意（可能為脫水、心衰竭體液滯留、不健康減重等訊號）

危險值（critical）處置規則【最高優先】：
- 遇到 priorityFindings 含 severity = "critical" 的項目時，不可使用「偏高」「略高」「多注意」「建議觀察」這類輕描淡寫的語氣
- heroReport.headline 必須直接指出數值已達危險範圍並要求就醫，例如：「血氧 85% 已嚴重缺氧，建議立即就醫」、「體溫 43°C 已達危險高燒，請立即就醫」
- heroReport.body 必須出現明確就醫動作語句（「立即就醫」「盡速送醫」「撥打 119」擇一），不可只說「多注意」「持續觀察」
- heroReport.tone 必須設為 "watchful"；confidence 設為 "high"
- keyInsight.severity 必須設為 "critical"（不可寫成 "high"）
- keyInsight.body 必須清楚指出這是危險範圍、具體身體風險（例如中風、休克、意識改變、呼吸衰竭）
- actionSuggestion.priority 必須為 "high"，timeframe 必須為 "today"
- actionSuggestion.body 必須是可立即執行的就醫／求救動作，禁止只寫「再量一次」「多注意飲食」
- alerts 陣列必須包含至少一個項目，其中 type = "health_alert"、severity = "critical"，message 必須直接提醒就醫並附上實際數值
- 若同時出現多個 critical，heroReport 與 keyInsight 先講最危險的那個，其他在 alerts 中補齊
- critical 情境的所有 body 禁止附文言尾韻或祝福語（如「願⋯⋯」「歲月靜好」「身心調和」），必須以就醫指引或具體動作收尾

嚴格規則：
- 所有字數限制都是「硬性上限」，超過會被裁切，請務必精簡
- 禁止把「近 7 天有幾筆紀錄」當成 body 的主體
- 禁止只重述數值或平均值
- 禁止在文字中出現英文欄位名（如 before_meal、systolic 等）
- 禁止寫無資訊量的填充句，例如：「持續監測很重要」「建議多多留意」「請多多注意」「需要持續關注」這類空話
- 每段 body 必須包含「實際數值」與「對應參考值」，讓家屬一眼看懂嚴重程度，但要用自然口吻說出來，不是條列式
- 即使只有 1 筆資料，也必須做對比，不可改寫成「持續觀察」「累積中」帶過
- 真的完全沒資料（0 筆）時才能說「累積中」

語氣準則（重要 — 避免醫療報告化）：
- 你是家庭護理師在對家屬說話，不是醫師在開診斷書
- 數值與參考值要「順著講進句子裡」，不要寫成「血氧 80%（正常 ≥95%）屬於需就醫區間」這種條列式
- 多用「比平常理想的⋯⋯低一點」「已經接近⋯⋯了」「離理想還差⋯⋯」這種日常比較，少用「落在⋯⋯區間」「屬於⋯⋯範圍」這類書面語
- 異常時要表達關心而非冰冷判讀，例如「需要先關心一下」「建議先別擔心，但⋯⋯」「我們可以一起多注意⋯⋯」
- 行動建議要像在叮嚀家人，不是寫處置流程，例如「先讓他坐下休息再量一次」優於「請休息靜坐後重測」
- 稱呼上若 Facts 內有提到家人稱謂或姓氏，可自然帶入（例如「王爺爺」），讓內容更貼近
- 非 critical 情境，body 結尾可選擇性附上一句溫潤的文言混搭關心語（如「身心調和，歲月靜好」「願你今日如常，從容自在」「心安即是歸處」），讓白話敘述帶有溫度尾韻；每段 body 最多一句，不可堆疊兩句以上，也不可整段都用文言寫
- critical 情境禁止附文言尾韻，以就醫指引收尾為主

結尾關懷語風格示範（可選，僅用於非 critical 的 body 結尾，每段最多一句）：
- 關懷型（偏異常、需要多留意的情境）：
  - 心安即是歸處，先照顧好自己的感受
  - 靜能養氣，緩則生柔
  - 把節奏放慢，身體自然會找到回家的路
  - 別擔心，我會陪你一起留意身體的變化
- 肯定型（穩定維持的情境）：
  - 身心調和，歲月靜好
  - 願你今日如常，從容自在
  - 寬心養氣，萬事皆順
  - 起居有常，身心自安
  - 安之若素，靜待花開
  - 持之以恆，便是長久之道
- 邀請型（資料偏少、邀請紀錄的情境）：
  - 心之所至，萬象皆新
  - 千里之行，始於足下
  - 一念起處，便是新的開始

請學習這種「白話開頭 + 文言尾韻」的混搭節奏；可從上方擇用，也可依語境改寫成相同風格的句子，但不可整段都寫成文言。

few-shot 風格示範（請嚴格遵守字數，並學習這種「有溫度的對比」寫法）：

示範 A：多指標異常（語氣偏關心提醒）
- heroReport.headline: 本週血壓與飯後血糖都偏高了
- heroReport.body: 這週血壓平均 138/88，比居家標準 135/85 高一些；飯後血糖也來到 180，建議我們一起調整。
- keyInsight.body: 血壓 138/88 已超過居家標準 135/85，飯後血糖 180 也偏高，兩個指標一起提醒我們要留意。
- actionSuggestion.body: 試試晚餐少一點澱粉，飯後陪著散步 10 分鐘，固定時段再量看看。
- todayCards[0].summary: 今天血壓 138/88，比居家標準 135/85 高一些，飯後散散步會更好。

示範 B：穩定維持中（語氣偏肯定鼓勵，示範白話 + 文言尾韻）
- heroReport.headline: 本週狀況穩定，維持得很好
- heroReport.body: 王爸爸這週血壓平均 118/76 在理想區間，體重也只差 0.5 公斤，身心調和，歲月靜好。
- keyInsight.body: 血壓和體重都維持在理想範圍裡，作息與飲食都顧得很好，安之若素。
- actionSuggestion.body: 維持現在的飲食和運動節奏，每天固定時段量測，便是長久之道。
- todayCards[0].summary: 今天血壓 118/76 還是很理想，照這個節奏走就好。

示範 C：只有 1 筆偏低資料（語氣偏溫和關心，但有具體動作；示範關懷型尾韻）
- heroReport.headline: 今天血氧偏低，需要先關心一下
- heroReport.body: 今天量到的血氧只有 94%，比理想的 95% 低一些，建議先讓他坐下休息再量一次，心安即是歸處。
- keyInsight.body: 血氧 94% 跟理想的 95% 差一點點，先觀察是否有呼吸不適，靜能養氣。
- actionSuggestion.body: 先讓他坐下休息幾分鐘再量一次，若持續偏低或不舒服就要就醫。
- todayCards[0].summary: 今天血氧 94% 比理想低一點，先休息再量一次。

示範 D：危險值 critical（語氣直接、明確指引就醫，禁止用「偏高／多注意」）
- heroReport.headline: 體溫 43°C 已達危險高燒，建議立即就醫
- heroReport.body: 今天量到 43°C 已遠超 39°C 的高燒警戒，可能伴隨意識模糊、脫水等高燒症狀，請立即就醫或撥打 119，不要在家僅靠退燒藥處理。
- keyInsight.label: 關鍵數據洞察
- keyInsight.body: 體溫 43°C 遠高於 37°C，屬醫療急症，持續高燒恐傷器官，必須立即處置。
- keyInsight.metricType: temperature
- keyInsight.severity: critical
- actionSuggestion.label: 健康行動建議
- actionSuggestion.body: 先協助散熱與補水，立即就醫或撥打 119，不建議在家觀察退燒。
- actionSuggestion.priority: high
- actionSuggestion.timeframe: today
- todayCards[0].summary: 今天體溫 43°C 已達危險高燒，請立即就醫，不要再等觀察。
- alerts[0].type: "health_alert"
- alerts[0].severity: "critical"
- alerts[0].message: 體溫 43°C 已達急症範圍，請立即就醫或撥打 119。

trendLabels 評語選擇規則（每個指標只能從以下五選一）：
- 「維持良好」：數據在理想區間且穩定
- 「逐步改善」：趨勢正在往好的方向走
- 「趨於穩定」：略偏離區間但持平、無惡化
- 「建議觀察」：數據偏離區間或波動偏大
- 「累積中」：資料筆數太少無法判讀

輸出要求（字數為硬性上限，請精簡，並務必把「數值 + 對比參考值」放入 body 內）：
- overallSummary：15-25 字，與 heroReport.headline 同方向
- todaySummary：25-35 字，與 todayCards[0].summary 同方向
- keyInsights：27-37 字，對應 keyInsight.body 第一重點
- recommendations：30-35 字，對應 actionSuggestion.body

- heroReport.headline：12-18 字，一句話講本週最重要的判讀
- heroReport.body：35-50 字，必須包含實際數值與對應參考區間的對比；非 critical 可在結尾接一句文言尾韻
- heroReport.tone：supportive / neutral / watchful
- heroReport.confidence：high / medium / low

- keyInsight.label：固定為「關鍵數據洞察」
- keyInsight.body：27-37 字，聚焦第一優先 finding，必須含數值對比（如「血氧 80% 低於正常 95%」）；非 critical 可在結尾接一句文言尾韻
- keyInsight.metricType：blood_pressure / blood_sugar / weight / temperature / blood_oxygen / general
- keyInsight.severity：low / medium / high / critical（critical 僅用於達到急症門檻的情境，見上方「危險值處置規則」）

- actionSuggestion.label：固定為「健康行動建議」
- actionSuggestion.body：30-35 字，必須對應 keyInsight，給出可執行動作；critical 情境必須給出就醫指引；非 critical 可在結尾接一句文言尾韻
- actionSuggestion.priority：low / medium / high（critical 情境固定為 high）
- actionSuggestion.timeframe：today / this_week（critical 情境固定為 today）

- todayCards：1-3 張
- todayCards[*].summary：25-35 字，口語化、含數值與參考區間，避免列出英文欄位名；critical 情境必須直接指出危險與就醫；今日卡片以即時提醒為主，盡量不附文言尾韻
- alerts：沒有提醒就回空陣列；critical 情境必須至少一筆 severity = "critical"、type = "health_alert" 的提醒
- alerts[*].severity：low / medium / high / critical
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
                                    description = "舊版相容欄位。與 heroReport.headline 同方向，15-25 字繁體中文。"
                                },
                                todaySummary = new
                                {
                                    type = "string",
                                    description = "舊版相容欄位。今日簡短回饋，25-35 字繁體中文。"
                                },
                                keyInsights = new
                                {
                                    type = "string",
                                    description = "舊版相容欄位。對應 keyInsight.body 第一重點，27-37 字。"
                                },
                                recommendations = new
                                {
                                    type = "string",
                                    description = "舊版相容欄位。與 actionSuggestion.body 保持一致，30-35 字。"
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
