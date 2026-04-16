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
                Cards = BuildTodayCards(today),
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

            var (insight, isFromCache, isFallback) = await GetOrGenerateInsightAsync(
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
                BloodPressureCount = bp.Count,
                BloodSugarCount = sugar.Count
            };
        }

        // ──────────────────────────────────────────────
        // AI 生成 / 快取
        // ──────────────────────────────────────────────

        private async Task<(AiGeneratedInsightDto? Insight, bool IsFromCache, bool IsFallback)> GetOrGenerateInsightAsync(
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
            if (cached != null)
            {
                return (MapInsight(cached), true, false);
            }

            try
            {
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

                return (insight, false, false);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex,
                    "AI 健康分析產生失敗，careGroupId={CareGroupId}，改用 fallback 內容。",
                    careGroupId);
                return (null, false, true);
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
            sb.Append($"共 {list.Count} 筆，最新 {latest.Systolic}/{latest.Diastolic} mmHg，");
            sb.Append($"平均 {avgSys:0.#}/{avgDia:0.#} mmHg，");
            sb.Append($"收縮壓範圍 {list.Min(x => x.Systolic)}-{list.Max(x => x.Systolic)}，");
            sb.Append($"舒張壓範圍 {list.Min(x => x.Diastolic)}-{list.Max(x => x.Diastolic)}，");
            sb.Append($"趨勢 {DescribeTrend(list.Select(x => (double)x.Systolic))}。");

            // 逐日摘要
            AppendDailySummary(sb, dateFrom, list, x => x.RecordDate,
                dayRecords => $"收縮壓 {dayRecords.Average(x => x.Systolic):0.#}/{dayRecords.Average(x => x.Diastolic):0.#}");

            return sb.ToString();
        }

        private static string BuildBloodSugarSummary(IReadOnlyList<BloodSugarRecord> records, DateTime dateFrom)
        {
            var list = records.OrderBy(x => x.RecordDate).ToList();
            if (list.Count == 0) return "近 7 天沒有血糖紀錄。";

            var latest = list[^1];
            var sb = new StringBuilder();
            sb.Append($"共 {list.Count} 筆，最新 {latest.GlucoseLevel:0.##} mg/dL，");
            sb.Append($"平均 {list.Average(x => x.GlucoseLevel):0.##} mg/dL，");
            sb.Append($"範圍 {list.Min(x => x.GlucoseLevel):0.##}-{list.Max(x => x.GlucoseLevel):0.##} mg/dL，");
            sb.Append($"趨勢 {DescribeTrend(list.Select(x => (double)x.GlucoseLevel))}。");

            // 量測情境分組
            var grouped = list
                .GroupBy(x => x.MeasurementContext)
                .Select(g => $"{g.Key}: {g.Count()} 筆, 平均 {g.Average(x => x.GlucoseLevel):0.##}")
                .ToList();
            if (grouped.Count > 0)
            {
                sb.Append($" 量測情境：{string.Join("；", grouped)}。");
            }

            // 逐日摘要
            AppendDailySummary(sb, dateFrom, list, x => x.RecordDate,
                dayRecords => $"平均 {dayRecords.Average(x => x.GlucoseLevel):0.##}");

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
            sb.Append($"共 {values.Count} 筆，最新 {values[^1]:0.##}{unit}，");
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
                dailyParts.Add($"{dateLabel}: {dayRecords.Count} 筆, {dayFormatter(dayRecords)}");
            }

            if (dailyParts.Count > 0)
            {
                sb.Append($" 逐日：{string.Join("；", dailyParts)}。");
            }
        }

        // ──────────────────────────────────────────────
        // 今日統計
        // ──────────────────────────────────────────────

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
            var types = 0;
            var timestamps = new List<DateTime>();

            void AddMetric<T>(IEnumerable<T> source, Func<T, DateTime> dateSelector, Func<T, string> formatter)
            {
                var todayRecords = source
                    .Where(x => dateSelector(x) >= todayStart && dateSelector(x) < end)
                    .OrderByDescending(dateSelector)
                    .ToList();
                if (todayRecords.Count == 0) return;
                timestamps.AddRange(todayRecords.Select(dateSelector));
                metrics.Add(formatter(todayRecords[0]));
                types++;
            }

            AddMetric(bp, x => x.RecordDate,
                x => $"{TitleBloodPressure} {x.Systolic}/{x.Diastolic} mmHg");
            AddMetric(sugar, x => x.RecordDate,
                x => $"{TitleBloodSugar} {x.GlucoseLevel.ToString("0.##", CultureInfo.InvariantCulture)} mg/dL");
            AddMetric(weight, x => x.RecordDate,
                x => $"{TitleWeight} {x.Value.ToString("0.##", CultureInfo.InvariantCulture)} kg");
            AddMetric(temp, x => x.RecordDate,
                x => $"{TitleTemperature} {x.Value.ToString("0.##", CultureInfo.InvariantCulture)} °C");
            AddMetric(oxygen, x => x.RecordDate,
                x => $"{TitleBloodOxygen} {x.SpO2.ToString("0.##", CultureInfo.InvariantCulture)}%");

            return new TodayMetricsResult
            {
                RecordCount = timestamps.Count,
                MetricTypeCount = types,
                LatestRecordAt = timestamps.Count > 0 ? timestamps.Max() : null,
                LatestMetrics = metrics
            };
        }

        // ──────────────────────────────────────────────
        // 今日卡片（規則產生，不依賴 AI）
        // ──────────────────────────────────────────────

        private static List<HealthDashboardTodayCardDto> BuildTodayCards(TodayMetricsResult today)
        {
            var progressPercent = (int)Math.Round(today.MetricTypeCount / 5.0 * 100, MidpointRounding.AwayFromZero);

            return new List<HealthDashboardTodayCardDto>
            {
                new()
                {
                    Title = TitleAiSummary,
                    Summary = today.RecordCount == 0
                        ? EmptyTodayInsight
                        : BuildTodayRecordSummary(today.RecordCount, today.LatestMetrics),
                    ProgressNote = $"今日健康任務達成 {progressPercent}%",
                    IconType = today.RecordCount == 0 ? "reminder" : "insight",
                    Tone = today.RecordCount == 0 ? "neutral" : "supportive"
                }
            };
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
                RulesVersion = RulesVersion
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
            public int BloodPressureCount { get; init; }
            public int BloodSugarCount { get; init; }
        }

        private sealed class TodayMetricsResult
        {
            public int RecordCount { get; init; }
            public int MetricTypeCount { get; init; }
            public DateTime? LatestRecordAt { get; init; }
            public IReadOnlyList<string> LatestMetrics { get; init; } = Array.Empty<string>();
        }
    }
}
