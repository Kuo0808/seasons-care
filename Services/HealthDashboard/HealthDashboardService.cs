using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using SeasonsCare.Api.Config;
using SeasonsCare.Api.DTOs.AiHealthInsights;
using SeasonsCare.Api.DTOs.HealthDashboard;
using SeasonsCare.Api.Exceptions;
using SeasonsCare.Api.Models.Entities;
using SeasonsCare.Api.Models.Entities.HealthRecords;
using SeasonsCare.Api.Repositories;
using SeasonsCare.Api.Repositories.HealthRecords;
using SeasonsCare.Api.Services.AI;

namespace SeasonsCare.Api.Services.HealthDashboard
{
    public class HealthDashboardService : IHealthDashboardService
    {
        private const string DashboardReportType = "health_dashboard_7d";
        private const string RulesVersion = "health-report-v1";
        private const string CurrentPromptVersion = "health-dashboard-v12";
        private const string EmptyTodayInsight = "今天尚無健康紀錄，新增量測後會顯示摘要。";
        private const string LabelKeyInsight = "關鍵數據洞察";
        private const string LabelActionSuggestion = "健康行動建議";
        private const string TitleAiSummary = "AI 分析摘要";
        private const string TitleBloodPressure = "血壓";
        private const string TitleBloodOxygen = "血氧";
        private const string TitleBloodSugar = "血糖";
        private const string TitleTemperature = "體溫";
        private const string TitleWeight = "體重";
        private const string StatusStable = "穩定";
        private const string StatusWatch = "需觀察";
        private const string StatusInsufficient = "累積中";
        private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

        private readonly ICareGroupRepository _careGroupRepository;
        private readonly IAiHealthInsightRepository _aiHealthInsightRepository;
        private readonly IAiHealthInsightService _aiHealthInsightService;
        private readonly IAiIntegrationService _aiIntegrationService;
        private readonly IBloodPressureRepository _bloodPressureRepository;
        private readonly IBloodSugarRepository _bloodSugarRepository;
        private readonly IWeightRepository _weightRepository;
        private readonly ITemperatureRepository _temperatureRepository;
        private readonly IBloodOxygenRepository _bloodOxygenRepository;
        private readonly ILogger<HealthDashboardService> _logger;

        public HealthDashboardService(
            ICareGroupRepository careGroupRepository,
            IAiHealthInsightRepository aiHealthInsightRepository,
            IAiHealthInsightService aiHealthInsightService,
            IAiIntegrationService aiIntegrationService,
            IBloodPressureRepository bloodPressureRepository,
            IBloodSugarRepository bloodSugarRepository,
            IWeightRepository weightRepository,
            ITemperatureRepository temperatureRepository,
            IBloodOxygenRepository bloodOxygenRepository,
            ILogger<HealthDashboardService> logger)
        {
            _careGroupRepository = careGroupRepository;
            _aiHealthInsightRepository = aiHealthInsightRepository;
            _aiHealthInsightService = aiHealthInsightService;
            _aiIntegrationService = aiIntegrationService;
            _bloodPressureRepository = bloodPressureRepository;
            _bloodSugarRepository = bloodSugarRepository;
            _weightRepository = weightRepository;
            _temperatureRepository = temperatureRepository;
            _bloodOxygenRepository = bloodOxygenRepository;
            _logger = logger;
        }

        // ──────────────────────────────────────────────
        // API 1：每週 AI 分析報告（呼叫 AI）
        // ──────────────────────────────────────────────

        public async Task<HealthDashboardWeeklyInsightResponse> GetWeeklyInsightAsync(Guid currentUserId, Guid careGroupId)
        {
            var context = await BuildWeeklyContextAsync(currentUserId, careGroupId);

            var hero = context.Insight?.HeroReport ?? BuildFallbackHeroReport(context);
            var insightSection = context.Insight?.KeyInsightSection ?? BuildFallbackKeyInsightSection(context);
            var actionSection = context.Insight?.ActionSuggestionSection ?? BuildFallbackActionSuggestionSection(context);

            return new HealthDashboardWeeklyInsightResponse
            {
                DateFrom = TimeHelper.ToTaiwanOffset(context.DateFrom),
                DateTo = TimeHelper.ToTaiwanOffset(context.DateTo),
                IsFromCache = context.IsFromCache,
                HeroReport = hero,
                KeyInsightSection = insightSection,
                ActionSuggestionSection = actionSection,
                Alerts = context.Insight?.Alerts ?? BuildAlerts(context),
                Meta = BuildMeta(context)
            };
        }

        // ──────────────────────────────────────────────
        // API 2：今日健康摘要（不呼叫 AI，即時統計）
        // ──────────────────────────────────────────────

        public async Task<HealthDashboardTodayInsightResponse> GetTodayInsightAsync(Guid currentUserId, Guid careGroupId)
        {
            await CheckMembershipAsync(careGroupId, currentUserId);

            var todayStart = NormalizeTimestamp(TimeHelper.GetTaiwanDateStartUtc());
            var todayEnd = NormalizeTimestamp(todayStart.AddDays(1).AddMilliseconds(-1));

            var bp = await _bloodPressureRepository.GetByCareGroupIdAndDateRangeAsync(careGroupId, todayStart, todayEnd);
            var sugar = await _bloodSugarRepository.GetByCareGroupIdAndDateRangeAsync(careGroupId, todayStart, todayEnd);
            var weight = await _weightRepository.GetByCareGroupIdAndDateRangeAsync(careGroupId, todayStart, todayEnd);
            var temp = await _temperatureRepository.GetByCareGroupIdAndDateRangeAsync(careGroupId, todayStart, todayEnd);
            var oxygen = await _bloodOxygenRepository.GetByCareGroupIdAndDateRangeAsync(careGroupId, todayStart, todayEnd);

            var today = GetTodayMetrics(todayStart, bp, sugar, weight, temp, oxygen);

            return new HealthDashboardTodayInsightResponse
            {
                HasTodayRecords = today.RecordCount > 0,
                RecordCount = today.RecordCount,
                LatestRecordAt = today.LatestRecordAt.HasValue
                    ? TimeHelper.ToTaiwanOffset(today.LatestRecordAt.Value)
                    : null,
                Cards = BuildTodayCardsV2(today),
                Meta = new HealthDashboardMetaDto
                {
                    IsFallback = false,
                    Confidence = today.MetricTypeCount >= 3 ? "high" : (today.RecordCount > 0 ? "medium" : "low"),
                    RulesVersion = RulesVersion
                }
            };
        }

        // ──────────────────────────────────────────────
        // API 3：近七天趨勢總覽（不呼叫 AI，即時統計）
        // ──────────────────────────────────────────────

        public async Task<HealthDashboardTrendOverviewResponse> GetTrendOverviewAsync(Guid currentUserId, Guid careGroupId)
        {
            await CheckMembershipAsync(careGroupId, currentUserId);

            var todayStart = NormalizeTimestamp(TimeHelper.GetTaiwanDateStartUtc());
            var dateFrom = NormalizeTimestamp(todayStart.AddDays(-6));
            var dateTo = NormalizeTimestamp(todayStart.AddDays(1).AddMilliseconds(-1));

            var bp = await _bloodPressureRepository.GetByCareGroupIdAndDateRangeAsync(careGroupId, dateFrom, dateTo);
            var sugar = await _bloodSugarRepository.GetByCareGroupIdAndDateRangeAsync(careGroupId, dateFrom, dateTo);
            var weight = await _weightRepository.GetByCareGroupIdAndDateRangeAsync(careGroupId, dateFrom, dateTo);
            var temp = await _temperatureRepository.GetByCareGroupIdAndDateRangeAsync(careGroupId, dateFrom, dateTo);
            var oxygen = await _bloodOxygenRepository.GetByCareGroupIdAndDateRangeAsync(careGroupId, dateFrom, dateTo);

            // 嘗試讀取已快取的 AI 趨勢標籤
            var cached = await _aiHealthInsightRepository.GetByUniqueKeyAsync(careGroupId, DashboardReportType, dateFrom, dateTo);
            TrendLabelsDto? cachedLabels = null;
            if (!string.IsNullOrWhiteSpace(cached?.TrendLabels))
            {
                try { cachedLabels = JsonSerializer.Deserialize<TrendLabelsDto>(cached.TrendLabels, JsonOptions); }
                catch { /* 反序列化失敗時使用 fallback 規則 */ }
            }

            return new HealthDashboardTrendOverviewResponse
            {
                DateFrom = TimeHelper.ToTaiwanOffset(dateFrom),
                DateTo = TimeHelper.ToTaiwanOffset(dateTo),
                Metrics = new List<HealthDashboardTrendCardResponse>
                {
                    BuildBloodPressureTrendCard(dateFrom, bp, cachedLabels?.BloodPressure),
                    BuildSingleMetricTrendCard(
                        dateFrom, "blood_oxygen", TitleBloodOxygen, "%",
                        cachedLabels?.BloodOxygen, oxygen,
                        x => x.RecordDate, x => (decimal?)x.SpO2, 0, ResolveBloodOxygenLabel),
                    BuildSingleMetricTrendCard(
                        dateFrom, "blood_sugar", TitleBloodSugar, "mg/dL",
                        cachedLabels?.BloodSugar, sugar,
                        x => x.RecordDate, x => (decimal?)x.GlucoseLevel, 0, ResolveBloodSugarLabel),
                    BuildSingleMetricTrendCard(
                        dateFrom, "temperature", TitleTemperature, "°C",
                        cachedLabels?.Temperature, temp,
                        x => x.RecordDate, x => (decimal?)x.Value, 1, ResolveTemperatureLabel),
                    BuildSingleMetricTrendCard(
                        dateFrom, "weight", TitleWeight, "kg",
                        cachedLabels?.Weight, weight,
                        x => x.RecordDate, x => (decimal?)x.Value, 1, ResolveWeightLabel)
                }
            };
        }

        // ──────────────────────────────────────────────
        // 歷史紀錄（不動）
        // ──────────────────────────────────────────────

        public async Task<SeasonsCare.Api.DTOs.Common.PagedResponse<HealthDashboardHistoryItemResponse>> GetHistoryAsync(
            Guid currentUserId, Guid careGroupId, int page, int pageSize)
        {
            await CheckMembershipAsync(careGroupId, currentUserId);

            var (items, totalCount) = await _aiHealthInsightRepository.GetPagedHistoryAsync(
                careGroupId, DashboardReportType, page, pageSize);

            var mapped = items.Select(x => new HealthDashboardHistoryItemResponse
            {
                Id = x.Id,
                DateFrom = TimeHelper.ToTaiwanOffset(x.DateFrom),
                DateTo = TimeHelper.ToTaiwanOffset(x.DateTo),
                OverallSummary = x.OverallSummary,
                GeneratedAt = TimeHelper.ToTaiwanOffset(x.GeneratedAt)
            }).ToList();

            return new SeasonsCare.Api.DTOs.Common.PagedResponse<HealthDashboardHistoryItemResponse>(
                mapped, totalCount, page, pageSize);
        }

        // ──────────────────────────────────────────────
        // API 1 專用：建立包含 AI 的完整 context
        // ──────────────────────────────────────────────

        private async Task<WeeklyContext> BuildWeeklyContextAsync(Guid currentUserId, Guid careGroupId)
        {
            await CheckMembershipAsync(careGroupId, currentUserId);

            var todayStart = NormalizeTimestamp(TimeHelper.GetTaiwanDateStartUtc());
            var dateFrom = NormalizeTimestamp(todayStart.AddDays(-6));
            var dateTo = NormalizeTimestamp(todayStart.AddDays(1).AddMilliseconds(-1));

            var bp = await _bloodPressureRepository.GetByCareGroupIdAndDateRangeAsync(careGroupId, dateFrom, dateTo);
            var sugar = await _bloodSugarRepository.GetByCareGroupIdAndDateRangeAsync(careGroupId, dateFrom, dateTo);
            var weight = await _weightRepository.GetByCareGroupIdAndDateRangeAsync(careGroupId, dateFrom, dateTo);
            var temp = await _temperatureRepository.GetByCareGroupIdAndDateRangeAsync(careGroupId, dateFrom, dateTo);
            var oxygen = await _bloodOxygenRepository.GetByCareGroupIdAndDateRangeAsync(careGroupId, dateFrom, dateTo);

            var today = GetTodayMetrics(todayStart, bp, sugar, weight, temp, oxygen);
            var total = bp.Count + sugar.Count + weight.Count + temp.Count + oxygen.Count;

            var (insight, isFromCache, isFallback, debugError) = await GetOrGenerateInsightAsync(
                currentUserId, careGroupId, dateFrom, dateTo, total, today.RecordCount,
                BuildTodayRecordSummary(today.RecordCount, today.LatestMetrics),
                bp, sugar, weight, temp, oxygen);

            return new WeeklyContext
            {
                DateFrom = dateFrom,
                DateTo = dateTo,
                TotalRecordCount = total,
                TodayRecordCount = today.RecordCount,
                TodayMetricTypeCount = today.MetricTypeCount,
                LatestTodayMetrics = today.LatestMetrics,
                Insight = insight,
                IsFromCache = isFromCache,
                IsFallback = isFallback,
                DebugError = debugError,
                BloodPressureCount = bp.Count,
                BloodSugarCount = sugar.Count
            };
        }

        // ──────────────────────────────────────────────
        // AI 生成 / 快取
        // ──────────────────────────────────────────────

        private async Task<(AiGeneratedInsightDto? Insight, bool IsFromCache, bool IsFallback, string? DebugError)> GetOrGenerateInsightAsync(
            Guid currentUserId, Guid careGroupId,
            DateTime dateFrom, DateTime dateTo,
            int totalRecordCount, int todayRecordCount, string todaySummary,
            IReadOnlyList<BloodPressureRecord> bp,
            IReadOnlyList<BloodSugarRecord> sugar,
            IReadOnlyList<WeightRecord> weight,
            IReadOnlyList<TemperatureRecord> temp,
            IReadOnlyList<BloodOxygenRecord> oxygen)
        {
            var cached = await _aiHealthInsightRepository.GetByUniqueKeyAsync(
                careGroupId, DashboardReportType, dateFrom, dateTo);
            if (cached != null && cached.PromptVersion == CurrentPromptVersion)
            {
                return (MapInsight(cached), true, false, null);
            }

            if (cached != null)
            {
                _logger.LogInformation(
                    "快取的 Prompt 版本 {OldVersion} 與目前版本 {NewVersion} 不同，將重新產生 AI 分析。careGroupId={CareGroupId}",
                    cached.PromptVersion, CurrentPromptVersion, careGroupId);
            }

            try
            {
                var priorityFindings = BuildPriorityFindings(bp, sugar, weight, temp, oxygen);
                var promptInput = new HealthInsightPromptInput
                {
                    CareGroupId = careGroupId,
                    DateFrom = dateFrom,
                    DateTo = dateTo,
                    TotalRecordCount = totalRecordCount,
                    TodayRecordCount = todayRecordCount,
                    TodaySummary = todaySummary,
                    BloodPressureSummary = BuildBloodPressureSummary(bp, dateFrom),
                    BloodSugarSummary = BuildBloodSugarSummary(sugar, dateFrom),
                    WeightSummary = BuildDetailedMetricSummary(
                        weight.OrderBy(x => x.RecordDate).ToList(),
                        x => x.RecordDate, x => (double)x.Value, "體重", "kg", dateFrom),
                    TemperatureSummary = BuildDetailedMetricSummary(
                        temp.OrderBy(x => x.RecordDate).ToList(),
                        x => x.RecordDate, x => (double)x.Value, "體溫", "°C", dateFrom),
                    BloodOxygenSummary = BuildDetailedMetricSummary(
                        oxygen.OrderBy(x => x.RecordDate).ToList(),
                        x => x.RecordDate, x => (double)x.SpO2, "血氧", "%", dateFrom)
                    , ClinicalSummary = BuildClinicalSummary(priorityFindings),
                    NarrativeDirective = BuildNarrativeDirective(priorityFindings),
                    FewShotScenarios = BuildFewShotScenarios(priorityFindings),
                    PriorityFindings = priorityFindings
                };

                var insight = await _aiIntegrationService.GenerateHealthInsightAsync(promptInput);

                // 將結構化結果序列化成 JSON 存入 ResultJson
                var structuredResult = new
                {
                    heroReport = insight.HeroReport,
                    keyInsightSection = insight.KeyInsightSection,
                    actionSuggestionSection = insight.ActionSuggestionSection,
                    todayCards = insight.TodayCards,
                    alerts = insight.Alerts
                };

                var saved = await _aiHealthInsightService.SaveInsightAsync(currentUserId, careGroupId,
                    new SaveAiHealthInsightRequest
                    {
                        ReportType = DashboardReportType,
                        DateFrom = dateFrom,
                        DateTo = dateTo,
                        OverallSummary = insight.OverallSummary,
                        TodaySummary = insight.TodaySummary,
                        KeyInsights = insight.KeyInsights,
                        Recommendations = insight.Recommendations,
                        TrendLabels = insight.TrendLabels != null
                            ? JsonSerializer.Serialize(insight.TrendLabels, JsonOptions)
                            : null,
                        ResultJson = JsonSerializer.Serialize(structuredResult, JsonOptions),
                        SourceDataHash = insight.SourceDataHash,
                        ModelName = insight.ModelName,
                        PromptVersion = insight.PromptVersion
                    });

                insight.ModelName = saved.ModelName;
                insight.PromptVersion = saved.PromptVersion;
                insight.GeneratedAt = saved.GeneratedAt.UtcDateTime;

                return (insight, false, false, null);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex,
                    "AI 健康分析產生失敗，careGroupId={CareGroupId}，改用 fallback 內容。",
                    careGroupId);
                var errorDetail = $"{ex.GetType().Name}: {ex.Message}";
                if (ex.InnerException != null)
                {
                    errorDetail += $" | Inner: {ex.InnerException.GetType().Name}: {ex.InnerException.Message}";
                }
                return (null, false, true, errorDetail);
            }
        }

        // ──────────────────────────────────────────────
        // 送給 AI 的豐富摘要
        // ──────────────────────────────────────────────

        private static string BuildBloodPressureSummary(IReadOnlyList<BloodPressureRecord> records, DateTime dateFrom)
        {
            var list = records.OrderBy(x => x.RecordDate).ToList();
            if (list.Count == 0) return "近 7 天沒有血壓紀錄。";

            var latest = list[^1];
            var avgSys = list.Average(x => x.Systolic);
            var avgDia = list.Average(x => x.Diastolic);

            var sb = new StringBuilder();
            sb.Append($"最新 {latest.Systolic}/{latest.Diastolic} mmHg，");
            sb.Append($"平均 {avgSys:0.#}/{avgDia:0.#} mmHg，");
            sb.Append($"收縮壓範圍 {list.Min(x => x.Systolic)}-{list.Max(x => x.Systolic)}，");
            sb.Append($"舒張壓範圍 {list.Min(x => x.Diastolic)}-{list.Max(x => x.Diastolic)}，");
            sb.Append($"趨勢 {DescribeTrend(list.Select(x => (double)x.Systolic))}。");

            // 逐日摘要
            AppendDailySummary(sb, dateFrom, list, x => x.RecordDate,
                dayRecords => $"收縮壓 {dayRecords.Average(x => x.Systolic):0.#}/{dayRecords.Average(x => x.Diastolic):0.#}");

            sb.Append($" 判讀：{BuildBloodPressureWeeklyInterpretation(list)}");
            return sb.ToString();
        }

        private static string BuildBloodSugarSummary(IReadOnlyList<BloodSugarRecord> records, DateTime dateFrom)
        {
            var list = records.OrderBy(x => x.RecordDate).ToList();
            if (list.Count == 0) return "近 7 天沒有血糖紀錄。";

            var latest = list[^1];
            var sb = new StringBuilder();
            sb.Append($"最新 {latest.GlucoseLevel:0.##} mg/dL，");
            sb.Append($"平均 {list.Average(x => x.GlucoseLevel):0.##} mg/dL，");
            sb.Append($"範圍 {list.Min(x => x.GlucoseLevel):0.##}-{list.Max(x => x.GlucoseLevel):0.##} mg/dL，");
            sb.Append($"趨勢 {DescribeTrend(list.Select(x => (double)x.GlucoseLevel))}。");

            // 量測情境分組
            var grouped = list
                .GroupBy(x => x.MeasurementContext)
                .Select(g => $"{g.Key}: 平均 {g.Average(x => x.GlucoseLevel):0.##}")
                .ToList();
            if (grouped.Count > 0)
            {
                sb.Append($" 量測情境：{string.Join("；", grouped)}。");
            }

            // 逐日摘要
            AppendDailySummary(sb, dateFrom, list, x => x.RecordDate,
                dayRecords => $"平均 {dayRecords.Average(x => x.GlucoseLevel):0.##}");

            sb.Append($" 判讀：{BuildBloodSugarWeeklyInterpretation(list)}");
            return sb.ToString();
        }

        private static string BuildDetailedMetricSummary<T>(
            IReadOnlyList<T> sortedRecords,
            Func<T, DateTime> dateSelector,
            Func<T, double> valueSelector,
            string metricName, string unit,
            DateTime dateFrom)
        {
            if (sortedRecords.Count == 0) return $"近 7 天沒有{metricName}紀錄。";

            var values = sortedRecords.Select(valueSelector).ToList();
            var sb = new StringBuilder();
            sb.Append($"最新 {values[^1]:0.##}{unit}，");
            sb.Append($"平均 {values.Average():0.##}{unit}，");
            sb.Append($"範圍 {values.Min():0.##}-{values.Max():0.##}{unit}，");
            sb.Append($"趨勢 {DescribeTrend(values)}。");

            // 逐日摘要
            AppendDailySummary(sb, dateFrom, sortedRecords, dateSelector,
                dayRecords => $"平均 {dayRecords.Select(valueSelector).Average():0.##}{unit}");

            return sb.ToString();
        }

        private static void AppendDailySummary<T>(
            StringBuilder sb, DateTime dateFrom,
            IReadOnlyList<T> records,
            Func<T, DateTime> dateSelector,
            Func<List<T>, string> dayFormatter)
        {
            var dailyParts = new List<string>();
            for (var offset = 0; offset < 7; offset++)
            {
                var dayStart = dateFrom.AddDays(offset);
                var dayEnd = dayStart.AddDays(1);
                var dayRecords = records.Where(x => dateSelector(x) >= dayStart && dateSelector(x) < dayEnd).ToList();
                if (dayRecords.Count == 0) continue;

                var dateLabel = TimeHelper.ToTaiwanTime(dayStart).ToString("MM/dd", CultureInfo.InvariantCulture);
                dailyParts.Add($"{dateLabel}: {dayFormatter(dayRecords)}");
            }

            if (dailyParts.Count > 0)
            {
                sb.Append($" 逐日：{string.Join("；", dailyParts)}。");
            }
        }

        // ──────────────────────────────────────────────
        // 今日統計
        // ──────────────────────────────────────────────

        private static List<HealthPriorityFindingPromptDto> BuildPriorityFindings(
            IReadOnlyList<BloodPressureRecord> bp,
            IReadOnlyList<BloodSugarRecord> sugar,
            IReadOnlyList<WeightRecord> weight,
            IReadOnlyList<TemperatureRecord> temp,
            IReadOnlyList<BloodOxygenRecord> oxygen)
        {
            var findings = new List<HealthPriorityFindingPromptDto>();

            AddFinding(findings, BuildBloodPressureFinding(bp));
            AddFinding(findings, BuildBloodSugarFinding(sugar));
            AddFinding(findings, BuildTemperatureFinding(temp));
            AddFinding(findings, BuildBloodOxygenFinding(oxygen));
            AddFinding(findings, BuildWeightFinding(weight));
            AddFinding(findings, BuildCombinedFinding(findings));

            return findings
                .OrderByDescending(x => GetSeverityRank(x.Severity))
                .ThenByDescending(x => GetConfidenceRank(x.Confidence))
                .ThenByDescending(x => x.IsMultiMetric)
                .ThenBy(x => x.MetricType)
                .Take(4)
                .ToList();
        }

        private static void AddFinding(List<HealthPriorityFindingPromptDto> findings, HealthPriorityFindingPromptDto? finding)
        {
            if (finding != null)
            {
                findings.Add(finding);
            }
        }

        private static HealthPriorityFindingPromptDto? BuildBloodPressureFinding(IReadOnlyList<BloodPressureRecord> records)
        {
            if (records.Count == 0) return null;

            var ordered = records.OrderBy(x => x.RecordDate).ToList();
            var latest = ordered[^1];
            var averageSystolic = (int)Math.Round(ordered.Average(x => x.Systolic), MidpointRounding.AwayFromZero);
            var averageDiastolic = (int)Math.Round(ordered.Average(x => x.Diastolic), MidpointRounding.AwayFromZero);
            var latestCategory = ClassifyBloodPressure(latest.Systolic, latest.Diastolic);
            var averageCategory = ClassifyBloodPressure(averageSystolic, averageDiastolic);

            return new HealthPriorityFindingPromptDto
            {
                MetricType = "blood_pressure",
                Severity = latestCategory == "high" ? "high" : (latestCategory == "elevated" ? "medium" : "low"),
                Confidence = ResolveFindingConfidence(ordered.Count),
                Title = latestCategory == "high" ? "血壓偏高需要先留意" : (latestCategory == "elevated" ? "血壓略高值得觀察" : "血壓大致穩定"),
                Evidence = ordered.Count == 1
                    ? $"近 7 天 1 筆血壓 {latest.Systolic}/{latest.Diastolic} mmHg。"
                    : $"近 7 天 {ordered.Count} 筆血壓，最新 {latest.Systolic}/{latest.Diastolic} mmHg，平均 {averageSystolic}/{averageDiastolic} mmHg。",
                Assessment = latestCategory == "high"
                    ? $"最新血壓已落在偏高區間，平均也接近 {MapBloodPressureCategory(averageCategory)}。"
                    : (latestCategory == "elevated"
                        ? $"最新血壓比理想區間高一些，整體接近 {MapBloodPressureCategory(averageCategory)}。"
                        : "目前血壓仍在可接受範圍，重點是維持量測節奏。"),
                SuggestedFocus = latestCategory == "normal"
                    ? "先肯定目前穩定，再提醒固定時段量測。"
                    : "先提醒休息後補量一次，再引導觀察接下來幾天是否持續偏高。"
            };
        }

        private static HealthPriorityFindingPromptDto? BuildBloodSugarFinding(IReadOnlyList<BloodSugarRecord> records)
        {
            if (records.Count == 0) return null;

            var ordered = records.OrderBy(x => x.RecordDate).ToList();
            var latest = ordered[^1];
            var measurementContext = (latest.MeasurementContext ?? string.Empty).Trim();
            var latestCategory = ClassifyBloodSugar(latest.GlucoseLevel, measurementContext);
            var average = ordered.Average(x => x.GlucoseLevel);

            return new HealthPriorityFindingPromptDto
            {
                MetricType = "blood_sugar",
                Severity = latestCategory == "high" || latestCategory == "low" ? "high" : "low",
                Confidence = ResolveFindingConfidence(ordered.Count),
                Title = latestCategory switch
                {
                    "high" => "血糖偏高需要控制飲食與量測節奏",
                    "low" => "血糖偏低需要先留意身體狀態",
                    _ => "血糖目前大致穩定"
                },
                Evidence = ordered.Count == 1
                    ? $"近 7 天 1 筆血糖 {latest.GlucoseLevel:0.##} mg/dL，情境為 {NormalizeMeasurementContext(measurementContext)}。"
                    : $"近 7 天 {ordered.Count} 筆血糖，最新 {latest.GlucoseLevel:0.##} mg/dL，平均 {average:0.##} mg/dL。",
                Assessment = latestCategory switch
                {
                    "high" => "這筆血糖已高於理想區間，值得先觀察飲食時間與飯後活動。",
                    "low" => "這筆血糖偏低，表達上要溫和提醒補充與觀察不適。",
                    _ => "血糖暫時沒有明顯警訊，可用肯定語氣帶出持續追蹤。"
                },
                SuggestedFocus = latestCategory == "normal"
                    ? "先肯定，再提醒記錄飯前或飯後情境。"
                    : "聚焦飲食、量測時機與是否需要補量確認。"
            };
        }

        private static HealthPriorityFindingPromptDto? BuildTemperatureFinding(IReadOnlyList<TemperatureRecord> records)
        {
            if (records.Count == 0) return null;

            var ordered = records.OrderBy(x => x.RecordDate).ToList();
            var latest = ordered[^1];
            var severity = latest.Value > 39m ? "high" : (latest.Value >= 37.3m ? "medium" : "low");

            return new HealthPriorityFindingPromptDto
            {
                MetricType = "temperature",
                Severity = severity,
                Confidence = ResolveFindingConfidence(ordered.Count),
                Title = severity == "high" ? "體溫偏高要留意是否持續發燒" : (severity == "medium" ? "體溫略高建議持續觀察" : "體溫暫時穩定"),
                Evidence = ordered.Count == 1
                    ? $"近 7 天 1 筆體溫 {latest.Value:0.##}°C。"
                    : $"近 7 天 {ordered.Count} 筆體溫，最新 {latest.Value:0.##}°C，平均 {ordered.Average(x => x.Value):0.##}°C。",
                Assessment = severity == "high"
                    ? "目前體溫已高於一般理想範圍，敘事要先聚焦持續觀察與不適症狀。"
                    : (severity == "medium"
                        ? "這筆體溫略高，適合提醒補水、休息與再次量測。"
                        : "體溫目前沒有明顯異常。"),
                SuggestedFocus = severity == "low"
                    ? "用肯定口吻提醒繼續觀察。"
                    : "提醒休息、補水與留意是否持續升高。"
            };
        }

        private static HealthPriorityFindingPromptDto? BuildBloodOxygenFinding(IReadOnlyList<BloodOxygenRecord> records)
        {
            if (records.Count == 0) return null;

            var ordered = records.OrderBy(x => x.RecordDate).ToList();
            var latest = ordered[^1];
            var severity = latest.SpO2 < 90m ? "high" : (latest.SpO2 < 95m ? "medium" : "low");

            return new HealthPriorityFindingPromptDto
            {
                MetricType = "blood_oxygen",
                Severity = severity,
                Confidence = ResolveFindingConfidence(ordered.Count),
                Title = severity == "high" ? "血氧偏低要優先注意" : (severity == "medium" ? "血氧略低建議持續留意" : "血氧大致穩定"),
                Evidence = ordered.Count == 1
                    ? $"近 7 天 1 筆血氧 {latest.SpO2:0.##}%。"
                    : $"近 7 天 {ordered.Count} 筆血氧，最新 {latest.SpO2:0.##}%，平均 {ordered.Average(x => x.SpO2):0.##}%。",
                Assessment = severity == "high"
                    ? "這筆血氧已低於一般理想區間，敘事要先講風險，再提醒觀察身體狀況。"
                    : (severity == "medium"
                        ? "血氧略低於理想值，適合提醒再次量測與留意呼吸狀況。"
                        : "血氧目前沒有明顯警訊。"),
                SuggestedFocus = severity == "low"
                    ? "以穩定表現描述即可。"
                    : "提醒再次量測並留意呼吸不適或活動後變化。"
            };
        }

        private static HealthPriorityFindingPromptDto? BuildWeightFinding(IReadOnlyList<WeightRecord> records)
        {
            if (records.Count == 0) return null;

            var ordered = records.OrderBy(x => x.RecordDate).ToList();
            var range = ordered.Max(x => x.Value) - ordered.Min(x => x.Value);
            var severity = range > 2m ? "medium" : "low";

            return new HealthPriorityFindingPromptDto
            {
                MetricType = "weight",
                Severity = severity,
                Confidence = ResolveFindingConfidence(ordered.Count),
                Title = severity == "medium" ? "體重波動較大需要持續觀察" : "體重變化大致平穩",
                Evidence = ordered.Count == 1
                    ? $"近 7 天 1 筆體重 {ordered[^1].Value:0.##} kg。"
                    : $"近 7 天 {ordered.Count} 筆體重，最新 {ordered[^1].Value:0.##} kg，一週變化約 {range:0.##} kg。",
                Assessment = severity == "medium"
                    ? "體重波動已超過一般日常變動，適合溫和提醒留意飲食與水分狀態。"
                    : "體重目前沒有明顯波動，適合用維持型敘事。",
                SuggestedFocus = severity == "medium"
                    ? "提醒固定時段量測體重，觀察是否持續變化。"
                    : "先肯定穩定，再提醒持續紀錄。"
            };
        }

        private static HealthPriorityFindingPromptDto? BuildCombinedFinding(IReadOnlyList<HealthPriorityFindingPromptDto> findings)
        {
            var bloodPressure = findings.FirstOrDefault(x => x.MetricType == "blood_pressure");
            var bloodSugar = findings.FirstOrDefault(x => x.MetricType == "blood_sugar");
            if (HasMediumOrHigher(bloodPressure) && HasMediumOrHigher(bloodSugar))
            {
                return new HealthPriorityFindingPromptDto
                {
                    MetricType = "general",
                    Severity = bloodPressure!.Severity == "high" || bloodSugar!.Severity == "high" ? "high" : "medium",
                    Confidence = MergeConfidence(bloodPressure!.Confidence, bloodSugar!.Confidence),
                    Title = "血壓與血糖都值得一起留意",
                    Evidence = $"{bloodPressure.Evidence} {bloodSugar.Evidence}",
                    Assessment = "這不是單一指標波動，建議敘事優先聚焦多指標一起偏高的提醒。",
                    SuggestedFocus = "先講最主要異常，再把飲食、作息與補量安排串成同一段建議。",
                    IsMultiMetric = true
                };
            }

            var temperature = findings.FirstOrDefault(x => x.MetricType == "temperature");
            var oxygen = findings.FirstOrDefault(x => x.MetricType == "blood_oxygen");
            if (HasMediumOrHigher(temperature) && HasMediumOrHigher(oxygen))
            {
                return new HealthPriorityFindingPromptDto
                {
                    MetricType = "general",
                    Severity = temperature!.Severity == "high" || oxygen!.Severity == "high" ? "high" : "medium",
                    Confidence = MergeConfidence(temperature!.Confidence, oxygen!.Confidence),
                    Title = "體溫與血氧需要一起觀察",
                    Evidence = $"{temperature.Evidence} {oxygen.Evidence}",
                    Assessment = "體溫與血氧同時偏離理想值時，文案要更聚焦身體狀態與持續觀察。",
                    SuggestedFocus = "提醒補量、留意不適與必要時尋求醫療協助。",
                    IsMultiMetric = true
                };
            }

            return null;
        }

        private static string BuildClinicalSummary(IReadOnlyList<HealthPriorityFindingPromptDto> findings)
        {
            if (findings.Count == 0)
            {
                return "目前沒有可判讀的指標異常，請用溫和邀請的口吻鼓勵開始累積第一筆健康紀錄。";
            }

            var topFindings = findings
                .OrderByDescending(x => GetSeverityRank(x.Severity))
                .ThenByDescending(x => GetConfidenceRank(x.Confidence))
                .Take(2)
                .ToList();

            if (GetSeverityRank(topFindings[0].Severity) >= GetSeverityRank("medium"))
            {
                return $"本次請優先聚焦：{string.Join("；", topFindings.Select(x => x.Title))}。避免先寒暄，先講判讀與下一步。";
            }

            return $"本次整體以穩定或輕微變化為主，請先肯定，再補上最值得持續觀察的重點：{topFindings[0].Title}。";
        }

        private static string BuildNarrativeDirective(IReadOnlyList<HealthPriorityFindingPromptDto> findings)
        {
            if (findings.Count == 0)
            {
                return "用陪伴式開場，邀請家屬從第一筆紀錄開始，避免系統訊息口吻。";
            }

            var topFinding = findings
                .OrderByDescending(x => GetSeverityRank(x.Severity))
                .ThenByDescending(x => GetConfidenceRank(x.Confidence))
                .First();

            return GetSeverityRank(topFinding.Severity) >= GetSeverityRank("medium")
                ? $"heroReport、keyInsight、actionSuggestion 都要優先圍繞「{topFinding.Title}」來寫。"
                : $"先肯定目前狀態，再以「{topFinding.Title}」做輕提醒，避免語氣過重。";
        }

        private static List<string> BuildFewShotScenarios(IReadOnlyList<HealthPriorityFindingPromptDto> findings)
        {
            var scenarios = new List<string>();
            if (findings.Any(x => x.IsMultiMetric))
            {
                scenarios.Add("multi_metric_abnormal");
            }

            if (findings.Any(x => x.Confidence == "low"))
            {
                scenarios.Add("single_or_sparse_reading");
            }

            if (findings.Count > 0 && findings.All(x => x.Severity == "low"))
            {
                scenarios.Add("stable_with_light_variation");
            }

            if (scenarios.Count == 0)
            {
                scenarios.Add("focused_single_metric");
            }

            return scenarios;
        }

        private static string NormalizeMeasurementContext(string context)
        {
            return string.IsNullOrWhiteSpace(context) ? "未標註情境" : context;
        }

        private static string ResolveFindingConfidence(int recordCount)
        {
            if (recordCount >= 6) return "high";
            if (recordCount >= 3) return "medium";
            return "low";
        }

        private static string MergeConfidence(string left, string right)
        {
            return GetConfidenceRank(left) <= GetConfidenceRank(right) ? left : right;
        }

        private static bool HasMediumOrHigher(HealthPriorityFindingPromptDto? finding)
        {
            return finding != null && GetSeverityRank(finding.Severity) >= GetSeverityRank("medium");
        }

        private static int GetSeverityRank(string severity)
        {
            return severity switch
            {
                "high" => 3,
                "medium" => 2,
                _ => 1
            };
        }

        private static int GetConfidenceRank(string confidence)
        {
            return confidence switch
            {
                "high" => 3,
                "medium" => 2,
                _ => 1
            };
        }

        private static string MapBloodPressureCategory(string category)
        {
            return category switch
            {
                "high" => "偏高區間",
                "elevated" => "略高區間",
                _ => "理想區間"
            };
        }

        private static TodayMetricsResult GetTodayMetrics(
            DateTime todayStart,
            IReadOnlyList<BloodPressureRecord> bp,
            IReadOnlyList<BloodSugarRecord> sugar,
            IReadOnlyList<WeightRecord> weight,
            IReadOnlyList<TemperatureRecord> temp,
            IReadOnlyList<BloodOxygenRecord> oxygen)
        {
            var end = todayStart.AddDays(1);
            var metrics = new List<string>();
            var interpretations = new List<TodayMetricInterpretation>();
            var types = 0;
            var timestamps = new List<DateTime>();

            void AddMetric<T>(
                IEnumerable<T> source,
                Func<T, DateTime> dateSelector,
                Func<T, string> formatter,
                Func<T, int, TodayMetricInterpretation>? interpretationFactory = null)
            {
                var todayRecords = source
                    .Where(x => dateSelector(x) >= todayStart && dateSelector(x) < end)
                    .OrderByDescending(dateSelector)
                    .ToList();
                if (todayRecords.Count == 0) return;
                timestamps.AddRange(todayRecords.Select(dateSelector));
                metrics.Add(formatter(todayRecords[0]));
                if (interpretationFactory != null)
                {
                    interpretations.Add(interpretationFactory(todayRecords[0], todayRecords.Count));
                }
                types++;
            }

            AddMetric(bp, x => x.RecordDate,
                x => $"{TitleBloodPressure} {x.Systolic}/{x.Diastolic} mmHg",
                BuildBloodPressureTodayInterpretation);
            AddMetric(sugar, x => x.RecordDate,
                x => $"{TitleBloodSugar} {x.GlucoseLevel.ToString("0.##", CultureInfo.InvariantCulture)} mg/dL",
                BuildBloodSugarTodayInterpretation);
            AddMetric(weight, x => x.RecordDate,
                x => $"{TitleWeight} {x.Value.ToString("0.##", CultureInfo.InvariantCulture)} kg",
                BuildWeightTodayInterpretation);
            AddMetric(temp, x => x.RecordDate,
                x => $"{TitleTemperature} {x.Value.ToString("0.##", CultureInfo.InvariantCulture)} °C");
            AddMetric(oxygen, x => x.RecordDate,
                x => $"{TitleBloodOxygen} {x.SpO2.ToString("0.##", CultureInfo.InvariantCulture)}%");

            var todayTemps = temp
                .Where(x => x.RecordDate >= todayStart && x.RecordDate < end)
                .OrderByDescending(x => x.RecordDate)
                .ToList();
            if (todayTemps.Count > 0)
            {
                interpretations.Add(BuildTemperatureTodayInterpretation(todayTemps[0], todayTemps.Count));
            }

            var todayOxygen = oxygen
                .Where(x => x.RecordDate >= todayStart && x.RecordDate < end)
                .OrderByDescending(x => x.RecordDate)
                .ToList();
            if (todayOxygen.Count > 0)
            {
                interpretations.Add(BuildBloodOxygenTodayInterpretation(todayOxygen[0], todayOxygen.Count));
            }

            return new TodayMetricsResult
            {
                RecordCount = timestamps.Count,
                MetricTypeCount = types,
                LatestRecordAt = timestamps.Count > 0 ? timestamps.Max() : null,
                LatestMetrics = metrics,
                Interpretations = interpretations
            };
        }

        // ──────────────────────────────────────────────
        // 今日卡片（規則產生，不依賴 AI）
        // ──────────────────────────────────────────────

        private static List<HealthDashboardTodayCardDto> BuildTodayCards(TodayMetricsResult today)
        {
            var progressPercent = (int)Math.Round(today.MetricTypeCount / 5.0 * 100, MidpointRounding.AwayFromZero);

            var cards = new List<HealthDashboardTodayCardDto>
            {
                new()
                {
                    Title = TitleAiSummary,
                    Summary = today.RecordCount == 0
                        ? EmptyTodayInsight
                        : BuildTodayRecordSummary(today.RecordCount, today.Interpretations, today.LatestMetrics),
                    ProgressNote = $"今日健康任務達成 {progressPercent}%",
                    IconType = today.RecordCount == 0 ? "reminder" : "insight",
                    Tone = today.RecordCount == 0 ? "neutral" : "supportive"
                }
            };

            return cards;
        }

        private static List<HealthDashboardTodayCardDto> BuildTodayCardsV2(TodayMetricsResult today)
        {
            var progressPercent = (int)Math.Round(today.MetricTypeCount / 5.0 * 100, MidpointRounding.AwayFromZero);
            var cards = new List<HealthDashboardTodayCardDto>
            {
                new()
                {
                    Title = TitleAiSummary,
                    Summary = today.RecordCount == 0
                        ? EmptyTodayInsight
                        : BuildTodayNarrative(today),
                    ProgressNote = BuildTodayProgressNarrative(today, progressPercent),
                    IconType = today.RecordCount == 0 ? "reminder" : "insight",
                    Tone = today.RecordCount == 0 ? "neutral" : "supportive"
                }
            };

            cards.AddRange(today.Interpretations
                .OrderByDescending(x => x.Priority)
                .ThenBy(x => x.Title)
                .Take(2)
                .Select(x => new HealthDashboardTodayCardDto
                {
                    Title = x.Title,
                    Summary = x.Summary,
                    ProgressNote = x.ProgressNote,
                    IconType = x.IconType,
                    Tone = x.Tone
                }));

            return cards;
        }

        private static string BuildTodayNarrative(TodayMetricsResult today)
        {
            if (today.Interpretations.Count == 0)
            {
                return BuildTodayRecordSummary(today.RecordCount, today.LatestMetrics);
            }

            var top = today.Interpretations
                .OrderByDescending(x => x.Priority)
                .ThenBy(x => x.Title)
                .Take(2)
                .ToList();

            if (today.RecordCount == 1)
            {
                return top[0].Summary;
            }

            if (top.Count == 1)
            {
                return $"今天共新增 {today.RecordCount} 筆健康紀錄，先看到的是：{top[0].Summary}";
            }

            return $"今天共新增 {today.RecordCount} 筆健康紀錄，目前最值得注意的是：{top[0].Summary} 另外，{top[1].Summary}";
        }

        private static string BuildTodayProgressNarrative(TodayMetricsResult today, int progressPercent)
        {
            if (today.RecordCount == 0)
            {
                return $"隞?亙熒隞餃??? {progressPercent}%";
            }

            var topPriority = today.Interpretations.Count == 0
                ? 0
                : today.Interpretations.Max(x => x.Priority);

            if (topPriority >= 4)
            {
                return "今天有較需要留意的數值，建議優先回量並留意是否持續不舒服。";
            }

            if (topPriority >= 2)
            {
                return "目前已有初步資料，再補 1 次同時段量測，判讀會更穩定。";
            }

            return $"今天已完成 {today.MetricTypeCount} 項指標紀錄，持續同時段量測會更容易比較變化。";
        }

        private static TodayMetricInterpretation BuildBloodPressureTodayInterpretation(BloodPressureRecord record, int todayCount)
        {
            var category = ClassifyBloodPressure(record.Systolic, record.Diastolic);
            var countHint = todayCount <= 1 ? "目前只有 1 筆，還不能直接當成趨勢。" : $"今天已有 {todayCount} 筆血壓紀錄，可一起觀察。";

            return new TodayMetricInterpretation
            {
                Title = TitleBloodPressure,
                Summary = category switch
                {
                    "high" => $"今天血壓 {record.Systolic}/{record.Diastolic} mmHg 明顯偏高，建議先安靜休息後再量一次。{countHint}",
                    "elevated" => $"今天血壓 {record.Systolic}/{record.Diastolic} mmHg 比理想區間高一點，先不用太緊張。{countHint}",
                    _ => $"今天血壓 {record.Systolic}/{record.Diastolic} mmHg 落在相對穩定的範圍，先把這次當作今天的基準值。"
                },
                ProgressNote = category switch
                {
                    "high" => "建議 10 到 15 分鐘後再量，若仍偏高可持續追蹤並留意不適。",
                    "elevated" => "建議固定在相近時段補量 1 次，看看是否只是當下狀態影響。",
                    _ => "維持固定時段量測，有助於後續觀察變化。"
                },
                IconType = category == "normal" ? "progress" : "insight",
                Tone = category == "high" ? "neutral" : "supportive",
                Priority = category switch
                {
                    "high" => 4,
                    "elevated" => 2,
                    _ => 1
                }
            };
        }

        private static TodayMetricInterpretation BuildBloodSugarTodayInterpretation(BloodSugarRecord record, int todayCount)
        {
            var context = (record.MeasurementContext ?? string.Empty).Trim();
            var category = ClassifyBloodSugar(record.GlucoseLevel, context);
            var contextLabel = string.IsNullOrWhiteSpace(context) ? "未註明情境" : context;
            var countHint = todayCount <= 1 ? "先把它當成提醒訊號，還需要後續紀錄幫忙判讀。" : $"今天已有 {todayCount} 筆血糖資料，可一起比較。";

            return new TodayMetricInterpretation
            {
                Title = TitleBloodSugar,
                Summary = category switch
                {
                    "high" => $"今天血糖 {record.GlucoseLevel:0.##} mg/dL（{contextLabel}）偏高一些，{countHint}",
                    "low" => $"今天血糖 {record.GlucoseLevel:0.##} mg/dL（{contextLabel}）偏低，建議先留意身體狀況並補記後續數值。",
                    _ => $"今天血糖 {record.GlucoseLevel:0.##} mg/dL（{contextLabel}）大致在可接受範圍，可持續觀察。"
                },
                ProgressNote = category switch
                {
                    "high" => "建議下次補上飯前或飯後情境，系統會更容易判斷是否持續偏高。",
                    "low" => "若有頭暈、冒冷汗等不適，請盡快處理並考慮尋求專業協助。",
                    _ => "固定紀錄量測情境，之後的分析會更準。"
                },
                IconType = category == "normal" ? "progress" : "insight",
                Tone = category == "low" ? "neutral" : "supportive",
                Priority = category switch
                {
                    "high" => 3,
                    "low" => 4,
                    _ => 1
                }
            };
        }

        private static TodayMetricInterpretation BuildWeightTodayInterpretation(WeightRecord record, int todayCount)
        {
            return new TodayMetricInterpretation
            {
                Title = TitleWeight,
                Summary = todayCount <= 1
                    ? $"今天體重 {record.Value:0.##} kg，先把這次當成近期比較的基準。"
                    : $"今天最新體重 {record.Value:0.##} kg，已有 {todayCount} 筆資料可和前面紀錄一起比較。",
                ProgressNote = "體重建議固定在相近時段量測，週趨勢通常比單筆更有判讀價值。",
                IconType = "progress",
                Tone = "supportive",
                Priority = 1
            };
        }

        private static TodayMetricInterpretation BuildTemperatureTodayInterpretation(TemperatureRecord record, int todayCount)
        {
            var category = record.Value >= 38m ? "high" : (record.Value >= 37.3m ? "elevated" : "normal");
            return new TodayMetricInterpretation
            {
                Title = TitleTemperature,
                Summary = category switch
                {
                    "high" => $"今天體溫 {record.Value:0.##}°C 已偏高，建議留意精神狀態與後續變化。",
                    "elevated" => $"今天體溫 {record.Value:0.##}°C 稍高，建議休息後再追一筆觀察。",
                    _ => $"今天體溫 {record.Value:0.##}°C 大致穩定，可持續記錄。"
                },
                ProgressNote = category == "normal"
                    ? "若今天有不舒服，仍可晚一點再補量一次。"
                    : "若後續持續升高或伴隨明顯不適，請提高警覺。",
                IconType = category == "normal" ? "progress" : "insight",
                Tone = category == "high" ? "neutral" : "supportive",
                Priority = category switch
                {
                    "high" => 4,
                    "elevated" => 2,
                    _ => 1
                }
            };
        }

        private static TodayMetricInterpretation BuildBloodOxygenTodayInterpretation(BloodOxygenRecord record, int todayCount)
        {
            var category = record.SpO2 < 92m ? "low" : (record.SpO2 < 95m ? "watch" : "normal");
            return new TodayMetricInterpretation
            {
                Title = TitleBloodOxygen,
                Summary = category switch
                {
                    "low" => $"今天血氧 {record.SpO2:0.##}% 偏低，建議盡快再次確認量測狀況並留意不適。",
                    "watch" => $"今天血氧 {record.SpO2:0.##}% 比理想值低一些，建議稍後再量一次確認。",
                    _ => $"今天血氧 {record.SpO2:0.##}% 在一般可接受範圍內。"
                },
                ProgressNote = category == "normal"
                    ? "持續同時段記錄，較容易看出穩定度。"
                    : "若重測仍偏低，請提高注意並視情況尋求協助。",
                IconType = category == "normal" ? "progress" : "insight",
                Tone = category == "low" ? "neutral" : "supportive",
                Priority = category switch
                {
                    "low" => 4,
                    "watch" => 3,
                    _ => 1
                }
            };
        }

        private static string ClassifyBloodPressure(int systolic, int diastolic)
        {
            if (systolic >= 140 || diastolic >= 90) return "high";
            if (systolic >= 120 || diastolic >= 80) return "elevated";
            return "normal";
        }

        private static string ClassifyBloodSugar(decimal value, string context)
        {
            var normalized = context.ToLowerInvariant();
            var isPostMeal = normalized.Contains("飯後") || normalized.Contains("餐後") || normalized.Contains("after");
            var isFasting = normalized.Contains("飯前") || normalized.Contains("空腹") || normalized.Contains("fast");

            if (isPostMeal)
            {
                if (value >= 200m) return "high";
                return value >= 140m ? "high" : "normal";
            }

            if (isFasting)
            {
                if (value < 70m) return "low";
                if (value >= 126m) return "high";
                return value >= 100m ? "high" : "normal";
            }

            if (value < 70m) return "low";
            return value > 140m ? "high" : "normal";
        }

        // ──────────────────────────────────────────────
        // 趨勢卡片
        // ──────────────────────────────────────────────

        private static HealthDashboardTrendCardResponse BuildBloodPressureTrendCard(
            DateTime dateFrom,
            IReadOnlyList<BloodPressureRecord> records,
            string? preferredLabel)
        {
            var values = records.OrderBy(x => x.RecordDate).ToList();
            var avgSys = values.Count == 0
                ? (decimal?)null
                : Math.Round((decimal)values.Average(x => x.Systolic), 0, MidpointRounding.AwayFromZero);
            var avgDia = values.Count == 0
                ? (decimal?)null
                : Math.Round((decimal)values.Average(x => x.Diastolic), 0, MidpointRounding.AwayFromZero);
            var latest = values.LastOrDefault();
            var status = ResolveBloodPressureLabel(preferredLabel, values);

            return new HealthDashboardTrendCardResponse
            {
                MetricType = "blood_pressure",
                Title = TitleBloodPressure,
                StatusLabel = status,
                Status = status,
                Summary = latest == null
                    ? "本週尚無血壓資料"
                    : $"{TitleBloodPressure}目前為{status}，最新數值 {latest.Systolic}/{latest.Diastolic} mmHg",
                DisplayValue = avgSys.HasValue && avgDia.HasValue
                    ? $"{avgSys.Value:0} / {avgDia.Value:0}"
                    : "-- / --",
                LatestValue = latest == null
                    ? "-- / --"
                    : $"{latest.Systolic}/{latest.Diastolic}",
                Change = values.Count >= 2
                    ? $"{values[^1].Systolic - values[0].Systolic:+#;-#;0}/{values[^1].Diastolic - values[0].Diastolic:+#;-#;0}"
                    : "0/0",
                TrendDirection = values.Count < 2
                    ? "steady"
                    : (values[^1].Systolic >= values[0].Systolic ? "up" : "down"),
                Unit = "mmHg",
                AverageValue = avgSys,
                SecondaryAverageValue = avgDia,
                Points = BuildDailyAveragePoints(dateFrom, values, x => x.RecordDate, x => (decimal?)x.Systolic),
                SecondaryPoints = BuildDailyAveragePoints(dateFrom, values, x => x.RecordDate, x => (decimal?)x.Diastolic)
            };
        }

        private static HealthDashboardTrendCardResponse BuildSingleMetricTrendCard<TRecord>(
            DateTime dateFrom, string metricType, string title, string unit,
            string? preferredLabel,
            IReadOnlyList<TRecord> records,
            Func<TRecord, DateTime> recordDateSelector,
            Func<TRecord, decimal?> valueSelector,
            int decimals,
            Func<string?, IReadOnlyList<decimal>, string> labelFactory)
        {
            var ordered = records.OrderBy(recordDateSelector).ToList();
            var values = ordered.Select(valueSelector).Where(x => x.HasValue).Select(x => x!.Value).ToList();
            var avg = values.Count == 0
                ? (decimal?)null
                : Math.Round(values.Average(), decimals, MidpointRounding.AwayFromZero);
            var latestStr = values.Count == 0
                ? "--"
                : values[^1].ToString($"0.{new string('#', decimals)}", CultureInfo.InvariantCulture);
            var status = labelFactory(preferredLabel, values);
            var decFormat = $"0.{new string('#', decimals)}";

            return new HealthDashboardTrendCardResponse
            {
                MetricType = metricType,
                Title = title,
                StatusLabel = status,
                Status = status,
                Summary = values.Count == 0
                    ? $"本週尚無{title}資料"
                    : $"{title}目前為{status}，最新數值 {latestStr} {unit}",
                DisplayValue = avg.HasValue
                    ? avg.Value.ToString(decFormat, CultureInfo.InvariantCulture)
                    : "--",
                LatestValue = latestStr,
                Change = values.Count >= 2
                    ? (values[^1] - values[0]).ToString($"+{decFormat};-{decFormat};0", CultureInfo.InvariantCulture)
                    : "0",
                TrendDirection = values.Count < 2
                    ? "steady"
                    : (values[^1] >= values[0] ? "up" : "down"),
                Unit = unit,
                AverageValue = avg,
                Points = BuildDailyAveragePoints(dateFrom, ordered, recordDateSelector, valueSelector)
            };
        }

        private static List<HealthDashboardTrendPointResponse> BuildDailyAveragePoints<TRecord>(
            DateTime dateFrom,
            IReadOnlyList<TRecord> records,
            Func<TRecord, DateTime> dateSelector,
            Func<TRecord, decimal?> valueSelector)
        {
            var points = new List<HealthDashboardTrendPointResponse>(7);
            for (var offset = 0; offset < 7; offset++)
            {
                var start = dateFrom.AddDays(offset);
                var end = start.AddDays(1);
                var dayValues = records
                    .Where(x => dateSelector(x) >= start && dateSelector(x) < end)
                    .Select(valueSelector)
                    .Where(x => x.HasValue)
                    .Select(x => x!.Value)
                    .ToList();

                points.Add(new HealthDashboardTrendPointResponse
                {
                    Date = TimeHelper.ToTaiwanTime(start).ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
                    Value = dayValues.Count == 0
                        ? null
                        : Math.Round(dayValues.Average(), 1, MidpointRounding.AwayFromZero)
                });
            }
            return points;
        }

        // ──────────────────────────────────────────────
        // Fallback 內容（AI 失敗時使用）
        // ──────────────────────────────────────────────

        private static HealthDashboardHeroReportDto BuildFallbackHeroReport(WeeklyContext context)
        {
            string headline;
            string body;
            if (context.TotalRecordCount == 0)
            {
                headline = "我們一起從今天開始為家人累積健康紀錄吧";
                body = "現在還沒有量測紀錄，第一筆就會是我們認識家人健康狀況的起點。每天固定時間量測，下週就能為您整理出更貼近的觀察。";
            }
            else if (context.TotalRecordCount < 3)
            {
                headline = "已經為家人留下健康足跡，請繼續陪我們累積";
                body = $"近 7 天目前有 {context.TotalRecordCount} 筆紀錄，已經是很好的開始。再多陪我們記下幾筆，就能看到更立體的健康樣貌，也更容易發現需要留意的變化。";
            }
            else
            {
                headline = $"本週已為家人累積 {context.TotalRecordCount} 筆紀錄，持續陪伴中";
                body = BuildTodayRecordSummary(context.TodayRecordCount, context.LatestTodayMetrics);
            }

            return new HealthDashboardHeroReportDto
            {
                Headline = headline,
                Body = body,
                Tone = context.TotalRecordCount == 0 ? "neutral" : "supportive",
                Confidence = ResolveConfidence(context)
            };
        }

        private static HealthDashboardInsightSectionDto BuildFallbackKeyInsightSection(WeeklyContext context)
        {
            string body;
            if (context.TotalRecordCount == 0)
            {
                body = "目前還沒有量測資料可以分析，第一筆紀錄會是我們陪伴的起點。";
            }
            else if (context.TotalRecordCount < 3)
            {
                body = $"目前累積到 {context.TotalRecordCount} 筆紀錄，已能看到家人量測的努力。再多陪我們累積幾天，就能畫出趨勢，也更容易抓到需要留意的變化。";
            }
            else if (context.TodayRecordCount > 0)
            {
                body = $"今天新增了 {context.TodayRecordCount} 筆紀錄，謝謝您持續為家人留心，我們會把它與近 7 天的趨勢一起觀察。";
            }
            else
            {
                body = $"近 7 天已累積 {context.TotalRecordCount} 筆紀錄，今日尚未量測，若方便為家人補上一筆，我們能更貼近他的當下狀況。";
            }

            return new HealthDashboardInsightSectionDto
            {
                Label = LabelKeyInsight,
                Body = body,
                MetricType = context.BloodPressureCount > 0 ? "blood_pressure"
                    : (context.BloodSugarCount > 0 ? "blood_sugar" : "general"),
                Severity = context.TotalRecordCount == 0 ? "medium" : "low"
            };
        }

        private static HealthDashboardActionSectionDto BuildFallbackActionSuggestionSection(WeeklyContext context)
        {
            string body;
            if (context.TotalRecordCount == 0)
            {
                body = "建議與家人約定一個固定的量測時段（例如早餐前），三天後我們就能一起看出初步的變化趨勢。";
            }
            else if (context.TotalRecordCount < 3)
            {
                body = "維持目前的量測節奏，若能在量測時順手記下飯前飯後或當下狀況，下次分析會更精準地陪您看見細節。";
            }
            else
            {
                body = "建議維持固定時段量測，並把飲食或作息變化也一起記下來，會讓我能更貼近地解讀數值的意義。";
            }

            return new HealthDashboardActionSectionDto
            {
                Label = LabelActionSuggestion,
                Body = body,
                Priority = context.TotalRecordCount == 0 ? "high" : "medium",
                Timeframe = context.TodayRecordCount > 0 ? "today" : "this_week"
            };
        }

        private static List<HealthDashboardAlertDto> BuildAlerts(WeeklyContext context)
        {
            var alerts = new List<HealthDashboardAlertDto>();

            if (context.TotalRecordCount == 0)
            {
                alerts.Add(new HealthDashboardAlertDto
                {
                    Type = "data_gap",
                    Message = "還沒有量測紀錄，邀請您為家人開始第一筆吧。",
                    Severity = "medium"
                });
            }
            else if (context.TodayRecordCount == 0)
            {
                alerts.Add(new HealthDashboardAlertDto
                {
                    Type = "reminder",
                    Message = "今日尚未新增量測，補一筆固定時段資料會更容易判讀趨勢。",
                    Severity = "low"
                });
            }

            return alerts;
        }

        // ──────────────────────────────────────────────
        // 共用工具方法
        // ──────────────────────────────────────────────

        private static HealthDashboardMetaDto BuildMeta(WeeklyContext context)
        {
            return new HealthDashboardMetaDto
            {
                IsFallback = context.IsFallback,
                Confidence = ResolveConfidence(context),
                ModelName = context.Insight?.ModelName,
                PromptVersion = context.Insight?.PromptVersion,
                RulesVersion = RulesVersion,
                DebugError = context.DebugError
            };
        }

        private static string ResolveConfidence(WeeklyContext context)
        {
            var metricTypesWithData = new[]
            {
                context.BloodPressureCount,
                context.BloodSugarCount,
                context.TotalRecordCount - context.BloodPressureCount - context.BloodSugarCount
            }.Count(x => x > 0);

            if (metricTypesWithData >= 4 && context.TotalRecordCount >= 8) return "high";
            if (metricTypesWithData >= 2 && context.TotalRecordCount >= 3) return "medium";
            return "low";
        }

        private static string BuildBloodPressureWeeklyInterpretation(IReadOnlyList<BloodPressureRecord> records)
        {
            var latest = records[^1];
            var avgSys = records.Average(x => x.Systolic);
            var avgDia = records.Average(x => x.Diastolic);
            var category = ClassifyBloodPressure((int)Math.Round(avgSys), (int)Math.Round(avgDia));

            if (records.Count == 1)
            {
                return category switch
                {
                    "high" => $"目前只有 1 筆血壓 {latest.Systolic}/{latest.Diastolic} mmHg，偏高，建議在休息後補量 1 次再判斷是否持續。",
                    "elevated" => $"目前只有 1 筆血壓 {latest.Systolic}/{latest.Diastolic} mmHg，略高於理想值，先持續追蹤即可。",
                    _ => $"目前只有 1 筆血壓 {latest.Systolic}/{latest.Diastolic} mmHg，落在相對穩定範圍。"
                };
            }

            return category switch
            {
                "high" => $"近 7 天平均血壓約 {avgSys:0.#}/{avgDia:0.#} mmHg，整體偏高，建議持續觀察是否反覆出現。",
                "elevated" => $"近 7 天平均血壓略高於理想值，建議固定時段量測，確認是否形成穩定趨勢。",
                _ => $"近 7 天血壓大致維持在相對穩定範圍。"
            };
        }

        private static string BuildBloodSugarWeeklyInterpretation(IReadOnlyList<BloodSugarRecord> records)
        {
            var latest = records[^1];
            var latestContext = (latest.MeasurementContext ?? string.Empty).Trim();
            var category = ClassifyBloodSugar(latest.GlucoseLevel, latestContext);

            if (records.Count == 1)
            {
                return category switch
                {
                    "low" => $"目前只有 1 筆血糖 {latest.GlucoseLevel:0.##} mg/dL，偏低，建議優先留意當下狀況並補記後續資料。",
                    "high" => $"目前只有 1 筆血糖 {latest.GlucoseLevel:0.##} mg/dL，偏高，建議搭配飯前或飯後情境再追蹤。",
                    _ => $"目前只有 1 筆血糖 {latest.GlucoseLevel:0.##} mg/dL，可先作為最近狀態的參考。"
                };
            }

            var avg = records.Average(x => x.GlucoseLevel);
            return avg > 140m
                ? $"近 7 天平均血糖約 {avg:0.##} mg/dL，整體偏高一些，建議持續補足量測情境。"
                : $"近 7 天血糖大致落在可接受範圍，可持續觀察。";
        }

        private static string DescribeTrend(IEnumerable<double> values)
        {
            var list = values.ToList();
            if (list.Count < 2) return "資料有限";
            var delta = list[^1] - list[0];
            if (Math.Abs(delta) < 0.01) return "趨於穩定";
            return delta > 0 ? "略為上升" : "略為下降";
        }

        private static string BuildTodayRecordSummary(int recordCount, IReadOnlyList<string> latestMetrics)
        {
            return recordCount == 0
                ? EmptyTodayInsight
                : $"今日共 {recordCount} 筆紀錄，{string.Join("、", latestMetrics)}。";
        }

        private static string BuildTodayRecordSummary(
            int recordCount,
            IReadOnlyList<TodayMetricInterpretation> interpretations,
            IReadOnlyList<string> latestMetrics)
        {
            if (interpretations.Count == 0)
            {
                return BuildTodayRecordSummary(recordCount, latestMetrics);
            }

            return interpretations
                .OrderByDescending(x => x.Priority)
                .ThenBy(x => x.Title)
                .First()
                .Summary;
        }

        private static string ResolveBloodPressureLabel(string? preferred, IReadOnlyList<BloodPressureRecord> records)
        {
            if (!string.IsNullOrWhiteSpace(preferred)) return preferred;
            if (records.Count == 0) return StatusInsufficient;
            return records.Average(x => x.Systolic) <= 130 && records.Average(x => x.Diastolic) <= 85
                ? StatusStable
                : StatusWatch;
        }

        private static string ResolveBloodOxygenLabel(string? preferred, IReadOnlyList<decimal> values)
        {
            if (!string.IsNullOrWhiteSpace(preferred)) return preferred;
            if (values.Count == 0) return StatusInsufficient;
            return values.Average() >= 95m ? StatusStable : StatusWatch;
        }

        private static string ResolveBloodSugarLabel(string? preferred, IReadOnlyList<decimal> values)
        {
            if (!string.IsNullOrWhiteSpace(preferred)) return preferred;
            if (values.Count == 0) return StatusInsufficient;
            return values.Average() <= 140m ? StatusStable : StatusWatch;
        }

        private static string ResolveTemperatureLabel(string? preferred, IReadOnlyList<decimal> values)
        {
            if (!string.IsNullOrWhiteSpace(preferred)) return preferred;
            if (values.Count == 0) return StatusInsufficient;
            var avg = values.Average();
            return avg >= 36m && avg <= 37.5m ? StatusStable : StatusWatch;
        }

        private static string ResolveWeightLabel(string? preferred, IReadOnlyList<decimal> values)
        {
            if (!string.IsNullOrWhiteSpace(preferred)) return preferred;
            if (values.Count == 0) return StatusInsufficient;
            return values.Max() - values.Min() <= 1m ? StatusStable : StatusWatch;
        }

        private async Task CheckMembershipAsync(Guid careGroupId, Guid currentUserId)
        {
            var isMember = await _careGroupRepository.IsMemberAsync(careGroupId, currentUserId);
            if (!isMember)
            {
                throw new DomainException("You are not a member of this care group.", "FORBIDDEN", 403);
            }
        }

        private static AiGeneratedInsightDto MapInsight(AiHealthInsight insight)
        {
            TrendLabelsDto? labels = null;
            if (!string.IsNullOrWhiteSpace(insight.TrendLabels))
            {
                try
                {
                    labels = JsonSerializer.Deserialize<TrendLabelsDto>(insight.TrendLabels, JsonOptions);
                }
                catch { /* 反序列化失敗時忽略 */ }
            }

            var dto = new AiGeneratedInsightDto
            {
                OverallSummary = insight.OverallSummary,
                TodaySummary = insight.TodaySummary,
                KeyInsights = insight.KeyInsights,
                Recommendations = insight.Recommendations,
                TrendLabels = labels,
                ModelName = insight.ModelName,
                PromptVersion = insight.PromptVersion,
                SourceDataHash = insight.SourceDataHash,
                GeneratedAt = insight.GeneratedAt
            };

            // 從 ResultJson 還原結構化內容
            if (!string.IsNullOrWhiteSpace(insight.ResultJson))
            {
                try
                {
                    using var doc = JsonDocument.Parse(insight.ResultJson);
                    var root = doc.RootElement;

                    if (root.TryGetProperty("heroReport", out var hero) && hero.ValueKind == JsonValueKind.Object)
                        dto.HeroReport = JsonSerializer.Deserialize<HealthDashboardHeroReportDto>(hero.GetRawText(), JsonOptions);

                    if (root.TryGetProperty("keyInsightSection", out var key) && key.ValueKind == JsonValueKind.Object)
                        dto.KeyInsightSection = JsonSerializer.Deserialize<HealthDashboardInsightSectionDto>(key.GetRawText(), JsonOptions);

                    if (root.TryGetProperty("actionSuggestionSection", out var action) && action.ValueKind == JsonValueKind.Object)
                        dto.ActionSuggestionSection = JsonSerializer.Deserialize<HealthDashboardActionSectionDto>(action.GetRawText(), JsonOptions);

                    if (root.TryGetProperty("todayCards", out var cards) && cards.ValueKind == JsonValueKind.Array)
                        dto.TodayCards = JsonSerializer.Deserialize<List<HealthDashboardTodayCardDto>>(cards.GetRawText(), JsonOptions) ?? new();

                    if (root.TryGetProperty("alerts", out var alerts) && alerts.ValueKind == JsonValueKind.Array)
                        dto.Alerts = JsonSerializer.Deserialize<List<HealthDashboardAlertDto>>(alerts.GetRawText(), JsonOptions) ?? new();
                }
                catch { /* ResultJson 反序列化失敗時使用 fallback */ }
            }

            return dto;
        }

        private static DateTime NormalizeTimestamp(DateTime value)
        {
            var utcValue = value.Kind == DateTimeKind.Utc ? value : value.ToUniversalTime();
            return new DateTime(utcValue.Ticks - (utcValue.Ticks % TimeSpan.TicksPerMillisecond), DateTimeKind.Utc);
        }

        // ──────────────────────────────────────────────
        // 內部資料結構
        // ──────────────────────────────────────────────

        private sealed class WeeklyContext
        {
            public DateTime DateFrom { get; init; }
            public DateTime DateTo { get; init; }
            public int TotalRecordCount { get; init; }
            public int TodayRecordCount { get; init; }
            public int TodayMetricTypeCount { get; init; }
            public IReadOnlyList<string> LatestTodayMetrics { get; init; } = Array.Empty<string>();
            public AiGeneratedInsightDto? Insight { get; init; }
            public bool IsFromCache { get; init; }
            public bool IsFallback { get; init; }
            public string? DebugError { get; init; }
            public int BloodPressureCount { get; init; }
            public int BloodSugarCount { get; init; }
        }

        private sealed class TodayMetricsResult
        {
            public int RecordCount { get; init; }
            public int MetricTypeCount { get; init; }
            public DateTime? LatestRecordAt { get; init; }
            public IReadOnlyList<string> LatestMetrics { get; init; } = Array.Empty<string>();
            public IReadOnlyList<TodayMetricInterpretation> Interpretations { get; init; } = Array.Empty<TodayMetricInterpretation>();
        }

        private sealed class TodayMetricInterpretation
        {
            public string Title { get; init; } = string.Empty;
            public string Summary { get; init; } = string.Empty;
            public string ProgressNote { get; init; } = string.Empty;
            public string IconType { get; init; } = "insight";
            public string Tone { get; init; } = "supportive";
            public int Priority { get; init; }
        }
    }
}
