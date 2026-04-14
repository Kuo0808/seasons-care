using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
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
        private const string EmptyTodayInsight = "當日尚未有紀錄，快來新增吧！";
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

        public async Task<HealthDashboardWeeklyInsightResponse> GetWeeklyInsightAsync(Guid currentUserId, Guid careGroupId)
        {
            var context = await BuildDashboardContextAsync(currentUserId, careGroupId);

            return new HealthDashboardWeeklyInsightResponse
            {
                OverallSummary = LimitText(
                    context.Insight?.OverallSummary ?? BuildFallbackOverallSummary(context.TotalRecordCount),
                    40),
                KeyInsight = LimitText(
                    context.Insight?.KeyInsights ?? BuildFallbackKeyInsight(context.TodayRecordCount, context.TotalRecordCount),
                    30),
                ActionSuggestion = context.Insight?.Recommendations ?? BuildFallbackActionSuggestion(context.TotalRecordCount),
                DateFrom = context.DateFrom,
                DateTo = context.DateTo,
                IsFromCache = context.IsFromCache
            };
        }

        public async Task<HealthDashboardTodayInsightResponse> GetTodayInsightAsync(Guid currentUserId, Guid careGroupId)
        {
            var context = await BuildDashboardContextAsync(currentUserId, careGroupId);

            if (context.TodayRecordCount == 0)
            {
                return new HealthDashboardTodayInsightResponse
                {
                    Summary = EmptyTodayInsight,
                    HasTodayRecords = false,
                    RecordCount = 0,
                    LatestRecordAt = null
                };
            }

            return new HealthDashboardTodayInsightResponse
            {
                Summary = !string.IsNullOrWhiteSpace(context.Insight?.TodaySummary)
                    ? context.Insight.TodaySummary
                    : BuildTodayRecordSummary(context.TodayRecordCount, context.LatestTodayMetrics),
                HasTodayRecords = true,
                RecordCount = context.TodayRecordCount,
                LatestRecordAt = context.LatestRecordAt
            };
        }

        public async Task<HealthDashboardTrendOverviewResponse> GetTrendOverviewAsync(Guid currentUserId, Guid careGroupId)
        {
            var context = await BuildDashboardContextAsync(currentUserId, careGroupId);

            return new HealthDashboardTrendOverviewResponse
            {
                DateFrom = context.DateFrom,
                DateTo = context.DateTo,
                Metrics = new List<HealthDashboardTrendCardResponse>
                {
                    BuildBloodPressureTrendCard(context),
                    BuildSingleMetricTrendCard(
                        dateFrom: context.DateFrom,
                        metricType: "blood_oxygen",
                        title: "血氧",
                        unit: "%",
                        preferredLabel: context.Insight?.TrendLabels?.BloodOxygen,
                        records: context.BloodOxygens,
                        recordDateSelector: x => x.RecordDate,
                        valueSelector: x => (decimal?)x.SpO2,
                        decimals: 0,
                        fallbackLabelFactory: ResolveBloodOxygenLabel),
                    BuildSingleMetricTrendCard(
                        dateFrom: context.DateFrom,
                        metricType: "blood_sugar",
                        title: "血糖",
                        unit: "mg/dL",
                        preferredLabel: context.Insight?.TrendLabels?.BloodSugar,
                        records: context.BloodSugars,
                        recordDateSelector: x => x.RecordDate,
                        valueSelector: x => (decimal?)x.GlucoseLevel,
                        decimals: 0,
                        fallbackLabelFactory: ResolveBloodSugarLabel),
                    BuildSingleMetricTrendCard(
                        dateFrom: context.DateFrom,
                        metricType: "temperature",
                        title: "體溫",
                        unit: "°C",
                        preferredLabel: context.Insight?.TrendLabels?.Temperature,
                        records: context.Temperatures,
                        recordDateSelector: x => x.RecordDate,
                        valueSelector: x => (decimal?)x.Value,
                        decimals: 1,
                        fallbackLabelFactory: ResolveTemperatureLabel),
                    BuildSingleMetricTrendCard(
                        dateFrom: context.DateFrom,
                        metricType: "weight",
                        title: "體重",
                        unit: "kg",
                        preferredLabel: context.Insight?.TrendLabels?.Weight,
                        records: context.Weights,
                        recordDateSelector: x => x.RecordDate,
                        valueSelector: x => (decimal?)x.Value,
                        decimals: 1,
                        fallbackLabelFactory: ResolveWeightLabel)
                }
            };
        }

        private async Task<DashboardContext> BuildDashboardContextAsync(Guid currentUserId, Guid careGroupId)
        {
            await CheckMembershipAsync(careGroupId, currentUserId);

            var todayStart = NormalizeTimestamp(TimeHelper.GetTaiwanDateStartUtc());
            var dateFrom = NormalizeTimestamp(todayStart.AddDays(-6));
            var dateTo = NormalizeTimestamp(todayStart.AddDays(1).AddMilliseconds(-1));

            var bloodPressures = await _bloodPressureRepository.GetByCareGroupIdAndDateRangeAsync(careGroupId, dateFrom, dateTo);
            var bloodSugars = await _bloodSugarRepository.GetByCareGroupIdAndDateRangeAsync(careGroupId, dateFrom, dateTo);
            var weights = await _weightRepository.GetByCareGroupIdAndDateRangeAsync(careGroupId, dateFrom, dateTo);
            var temperatures = await _temperatureRepository.GetByCareGroupIdAndDateRangeAsync(careGroupId, dateFrom, dateTo);
            var bloodOxygens = await _bloodOxygenRepository.GetByCareGroupIdAndDateRangeAsync(careGroupId, dateFrom, dateTo);

            var totalRecordCount = bloodPressures.Count + bloodSugars.Count + weights.Count + temperatures.Count + bloodOxygens.Count;
            var todayMetrics = GetTodayMetrics(todayStart, bloodPressures, bloodSugars, weights, temperatures, bloodOxygens);

            var insightResult = await GetOrGenerateInsightAsync(
                currentUserId,
                careGroupId,
                dateFrom,
                dateTo,
                BuildTodayRecordSummary(todayMetrics.RecordCount, todayMetrics.LatestMetrics),
                bloodPressures,
                bloodSugars,
                weights,
                temperatures,
                bloodOxygens);

            return new DashboardContext
            {
                DateFrom = dateFrom,
                DateTo = dateTo,
                Insight = insightResult.Insight,
                IsFromCache = insightResult.IsFromCache,
                TotalRecordCount = totalRecordCount,
                TodayRecordCount = todayMetrics.RecordCount,
                LatestRecordAt = todayMetrics.LatestRecordAt,
                LatestTodayMetrics = todayMetrics.LatestMetrics,
                BloodPressures = bloodPressures,
                BloodSugars = bloodSugars,
                Weights = weights,
                Temperatures = temperatures,
                BloodOxygens = bloodOxygens
            };
        }

        private async Task<(AiGeneratedInsightDto? Insight, bool IsFromCache)> GetOrGenerateInsightAsync(
            Guid currentUserId,
            Guid careGroupId,
            DateTime dateFrom,
            DateTime dateTo,
            string todaySummary,
            IEnumerable<BloodPressureRecord> bloodPressures,
            IEnumerable<BloodSugarRecord> bloodSugars,
            IEnumerable<WeightRecord> weights,
            IEnumerable<TemperatureRecord> temperatures,
            IEnumerable<BloodOxygenRecord> bloodOxygens)
        {
            var cachedInsight = await _aiHealthInsightRepository.GetByUniqueKeyAsync(careGroupId, DashboardReportType, dateFrom, dateTo);
            if (cachedInsight != null)
            {
                return (MapInsight(cachedInsight), true);
            }

            try
            {
                var promptInput = BuildPromptInput(careGroupId, dateFrom, dateTo, todaySummary, bloodPressures, bloodSugars, weights, temperatures, bloodOxygens);
                var generatedInsight = await _aiIntegrationService.GenerateHealthInsightAsync(promptInput);

                var savedInsight = await _aiHealthInsightService.SaveInsightAsync(currentUserId, careGroupId, new SaveAiHealthInsightRequest
                {
                    ReportType = DashboardReportType,
                    DateFrom = dateFrom,
                    DateTo = dateTo,
                    OverallSummary = generatedInsight.OverallSummary,
                    TodaySummary = generatedInsight.TodaySummary,
                    KeyInsights = generatedInsight.KeyInsights,
                    Recommendations = generatedInsight.Recommendations,
                    TrendLabels = generatedInsight.TrendLabels != null
                        ? JsonSerializer.Serialize(generatedInsight.TrendLabels, JsonOptions)
                        : null,
                    SourceDataHash = generatedInsight.SourceDataHash,
                    ModelName = generatedInsight.ModelName,
                    PromptVersion = generatedInsight.PromptVersion
                });

                return (new AiGeneratedInsightDto
                {
                    OverallSummary = savedInsight.OverallSummary,
                    TodaySummary = savedInsight.TodaySummary,
                    KeyInsights = savedInsight.KeyInsights,
                    Recommendations = savedInsight.Recommendations,
                    TrendLabels = generatedInsight.TrendLabels,
                    SourceDataHash = savedInsight.SourceDataHash,
                    ModelName = savedInsight.ModelName,
                    PromptVersion = savedInsight.PromptVersion,
                    GeneratedAt = savedInsight.GeneratedAt
                }, false);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to generate health dashboard AI insight for careGroupId {CareGroupId}. Returning fallback content.", careGroupId);
                return (null, false);
            }
        }

        private async Task CheckMembershipAsync(Guid careGroupId, Guid currentUserId)
        {
            var isMember = await _careGroupRepository.IsMemberAsync(careGroupId, currentUserId);
            if (!isMember)
            {
                throw new DomainException("You are not a member of this care group.", "FORBIDDEN", 403);
            }
        }

        private static TodayMetricsResult GetTodayMetrics(
            DateTime todayStart,
            IEnumerable<BloodPressureRecord> bloodPressures,
            IEnumerable<BloodSugarRecord> bloodSugars,
            IEnumerable<WeightRecord> weights,
            IEnumerable<TemperatureRecord> temperatures,
            IEnumerable<BloodOxygenRecord> bloodOxygens)
        {
            var todayEndExclusive = todayStart.AddDays(1);

            var todayBloodPressures = bloodPressures.Where(x => x.RecordDate >= todayStart && x.RecordDate < todayEndExclusive).OrderByDescending(x => x.RecordDate).ToList();
            var todayBloodSugars = bloodSugars.Where(x => x.RecordDate >= todayStart && x.RecordDate < todayEndExclusive).OrderByDescending(x => x.RecordDate).ToList();
            var todayWeights = weights.Where(x => x.RecordDate >= todayStart && x.RecordDate < todayEndExclusive).OrderByDescending(x => x.RecordDate).ToList();
            var todayTemperatures = temperatures.Where(x => x.RecordDate >= todayStart && x.RecordDate < todayEndExclusive).OrderByDescending(x => x.RecordDate).ToList();
            var todayBloodOxygens = bloodOxygens.Where(x => x.RecordDate >= todayStart && x.RecordDate < todayEndExclusive).OrderByDescending(x => x.RecordDate).ToList();

            var allTodayTimestamps = todayBloodPressures.Select(x => x.RecordDate)
                .Concat(todayBloodSugars.Select(x => x.RecordDate))
                .Concat(todayWeights.Select(x => x.RecordDate))
                .Concat(todayTemperatures.Select(x => x.RecordDate))
                .Concat(todayBloodOxygens.Select(x => x.RecordDate))
                .ToList();

            var metrics = new List<string>();

            if (todayBloodPressures.Count > 0)
            {
                var latest = todayBloodPressures[0];
                metrics.Add($"血壓 {latest.Systolic}/{latest.Diastolic} mmHg");
            }

            if (todayBloodSugars.Count > 0)
            {
                var latest = todayBloodSugars[0];
                metrics.Add($"血糖 {latest.GlucoseLevel.ToString("0.##", CultureInfo.InvariantCulture)} mg/dL");
            }

            if (todayWeights.Count > 0)
            {
                var latest = todayWeights[0];
                metrics.Add($"體重 {latest.Value.ToString("0.##", CultureInfo.InvariantCulture)} kg");
            }

            if (todayTemperatures.Count > 0)
            {
                var latest = todayTemperatures[0];
                metrics.Add($"體溫 {latest.Value.ToString("0.##", CultureInfo.InvariantCulture)} °C");
            }

            if (todayBloodOxygens.Count > 0)
            {
                var latest = todayBloodOxygens[0];
                metrics.Add($"血氧 {latest.SpO2.ToString("0.##", CultureInfo.InvariantCulture)}%");
            }

            return new TodayMetricsResult
            {
                RecordCount = allTodayTimestamps.Count,
                LatestRecordAt = allTodayTimestamps.Count > 0 ? allTodayTimestamps.Max() : null,
                LatestMetrics = metrics
            };
        }

        private static HealthInsightPromptInput BuildPromptInput(
            Guid careGroupId,
            DateTime dateFrom,
            DateTime dateTo,
            string todaySummary,
            IEnumerable<BloodPressureRecord> bloodPressures,
            IEnumerable<BloodSugarRecord> bloodSugars,
            IEnumerable<WeightRecord> weights,
            IEnumerable<TemperatureRecord> temperatures,
            IEnumerable<BloodOxygenRecord> bloodOxygens)
        {
            return new HealthInsightPromptInput
            {
                CareGroupId = careGroupId,
                DateFrom = dateFrom,
                DateTo = dateTo,
                TodaySummary = todaySummary,
                BloodPressureSummary = BuildBloodPressureSummary(bloodPressures),
                BloodSugarSummary = BuildBloodSugarSummary(bloodSugars),
                WeightSummary = BuildSingleMetricSummary(weights.Select(x => (double)x.Value), "體重", "kg"),
                TemperatureSummary = BuildSingleMetricSummary(temperatures.Select(x => (double)x.Value), "體溫", "°C"),
                BloodOxygenSummary = BuildSingleMetricSummary(bloodOxygens.Select(x => (double)x.SpO2), "血氧", "%")
            };
        }

        private static string BuildBloodPressureSummary(IEnumerable<BloodPressureRecord> records)
        {
            var list = records.OrderBy(x => x.RecordDate).ToList();
            if (list.Count == 0)
            {
                return "近 7 天沒有血壓紀錄。";
            }

            var latest = list[^1];
            var avgSystolic = list.Average(x => x.Systolic);
            var avgDiastolic = list.Average(x => x.Diastolic);
            var minSystolic = list.Min(x => x.Systolic);
            var maxSystolic = list.Max(x => x.Systolic);
            var minDiastolic = list.Min(x => x.Diastolic);
            var maxDiastolic = list.Max(x => x.Diastolic);

            return $"共 {list.Count} 筆，最新 {latest.Systolic}/{latest.Diastolic} mmHg，平均 {avgSystolic:0.#}/{avgDiastolic:0.#} mmHg，收縮壓範圍 {minSystolic}-{maxSystolic}，舒張壓範圍 {minDiastolic}-{maxDiastolic}，趨勢 {DescribeTrend(list.Select(x => (double)x.Systolic))}。";
        }

        private static string BuildBloodSugarSummary(IEnumerable<BloodSugarRecord> records)
        {
            var list = records.OrderBy(x => x.RecordDate).ToList();
            if (list.Count == 0)
            {
                return "近 7 天沒有血糖紀錄。";
            }

            var latest = list[^1];
            var groupedContexts = list
                .GroupBy(x => x.MeasurementContext)
                .Select(x => $"{x.Key}:{x.Count()} 筆")
                .ToList();

            return $"共 {list.Count} 筆，最新 {latest.GlucoseLevel:0.##} mg/dL，平均 {list.Average(x => x.GlucoseLevel):0.##} mg/dL，範圍 {list.Min(x => x.GlucoseLevel):0.##}-{list.Max(x => x.GlucoseLevel):0.##} mg/dL，量測情境 {string.Join("、", groupedContexts)}，趨勢 {DescribeTrend(list.Select(x => (double)x.GlucoseLevel))}。";
        }

        private static string BuildSingleMetricSummary(IEnumerable<double> values, string metricName, string unit)
        {
            var list = values.ToList();
            if (list.Count == 0)
            {
                return $"近 7 天沒有{metricName}紀錄。";
            }

            return $"共 {list.Count} 筆，最新 {list[^1]:0.##}{unit}，平均 {list.Average():0.##}{unit}，範圍 {list.Min():0.##}-{list.Max():0.##}{unit}，趨勢 {DescribeTrend(list)}。";
        }

        private static string DescribeTrend(IEnumerable<double> values)
        {
            var list = values.ToList();
            if (list.Count < 2)
            {
                return "資料有限";
            }

            var delta = list[^1] - list[0];
            if (Math.Abs(delta) < 0.01)
            {
                return "趨於穩定";
            }

            return delta > 0 ? "略為上升" : "略為下降";
        }

        private static string BuildTodayRecordSummary(int recordCount, IReadOnlyList<string> latestMetrics)
        {
            return recordCount == 0
                ? EmptyTodayInsight
                : $"今日共有 {recordCount} 筆健康紀錄：" + string.Join("、", latestMetrics);
        }

        private static string BuildFallbackOverallSummary(int totalRecordCount)
        {
            return totalRecordCount == 0
                ? "近 7 天尚無健康紀錄，先新增資料再觀察趨勢。"
                : $"近 7 天共有 {totalRecordCount} 筆健康紀錄，建議持續追蹤變化。";
        }

        private static string BuildFallbackKeyInsight(int todayRecordCount, int totalRecordCount)
        {
            if (todayRecordCount > 0)
            {
                return $"今日新增 {todayRecordCount} 筆健康紀錄。";
            }

            return totalRecordCount == 0
                ? "目前尚無可分析的健康資料。"
                : $"近 7 天共累積 {totalRecordCount} 筆紀錄。";
        }

        private static string BuildFallbackActionSuggestion(int totalRecordCount)
        {
            return totalRecordCount == 0
                ? "建議先建立固定量測習慣，之後才能得到更準確的分析建議。"
                : "建議維持固定量測時段，並同步記錄飲食與作息變化。";
        }

        private static string LimitText(string value, int maxLength)
        {
            var trimmed = value.Trim();
            return trimmed.Length <= maxLength ? trimmed : trimmed[..maxLength];
        }

        private static HealthDashboardTrendCardResponse BuildBloodPressureTrendCard(DashboardContext context)
        {
            decimal? averageSystolic = context.BloodPressures.Count == 0
                ? null
                : Math.Round((decimal)context.BloodPressures.Average(x => x.Systolic), 0, MidpointRounding.AwayFromZero);
            decimal? averageDiastolic = context.BloodPressures.Count == 0
                ? null
                : Math.Round((decimal)context.BloodPressures.Average(x => x.Diastolic), 0, MidpointRounding.AwayFromZero);

            return new HealthDashboardTrendCardResponse
            {
                MetricType = "blood_pressure",
                Title = "血壓",
                StatusLabel = ResolveBloodPressureLabel(context.Insight?.TrendLabels?.BloodPressure, context.BloodPressures),
                DisplayValue = averageSystolic.HasValue && averageDiastolic.HasValue
                    ? $"{averageSystolic.Value:0} / {averageDiastolic.Value:0}"
                    : "-- / --",
                Unit = "mmHg",
                AverageValue = averageSystolic,
                SecondaryAverageValue = averageDiastolic,
                Points = BuildDailyAveragePoints(context.DateFrom, context.BloodPressures, x => x.RecordDate, x => (decimal?)x.Systolic),
                SecondaryPoints = BuildDailyAveragePoints(context.DateFrom, context.BloodPressures, x => x.RecordDate, x => (decimal?)x.Diastolic)
            };
        }

        private static HealthDashboardTrendCardResponse BuildSingleMetricTrendCard<TRecord>(
            DateTime dateFrom,
            string metricType,
            string title,
            string unit,
            string? preferredLabel,
            IReadOnlyList<TRecord> records,
            Func<TRecord, DateTime> recordDateSelector,
            Func<TRecord, decimal?> valueSelector,
            int decimals,
            Func<string?, IReadOnlyList<decimal>, string> fallbackLabelFactory)
        {
            var validValues = records
                .Select(valueSelector)
                .Where(x => x.HasValue)
                .Select(x => x!.Value)
                .ToList();

            decimal? averageValue = validValues.Count == 0
                ? null
                : Math.Round(validValues.Average(), decimals, MidpointRounding.AwayFromZero);

            return new HealthDashboardTrendCardResponse
            {
                MetricType = metricType,
                Title = title,
                StatusLabel = fallbackLabelFactory(preferredLabel, validValues),
                DisplayValue = averageValue.HasValue
                    ? averageValue.Value.ToString($"0.{new string('#', decimals)}", CultureInfo.InvariantCulture)
                    : "--",
                Unit = unit,
                AverageValue = averageValue,
                Points = BuildDailyAveragePoints(dateFrom, records, recordDateSelector, valueSelector)
            };
        }

        private static List<HealthDashboardTrendPointResponse> BuildDailyAveragePoints<TRecord>(
            DateTime dateFrom,
            IReadOnlyList<TRecord> records,
            Func<TRecord, DateTime> recordDateSelector,
            Func<TRecord, decimal?> valueSelector)
        {
            var points = new List<HealthDashboardTrendPointResponse>(7);

            for (var dayOffset = 0; dayOffset < 7; dayOffset++)
            {
                var dayStart = dateFrom.AddDays(dayOffset);
                var dayEnd = dayStart.AddDays(1);
                var dayValues = records
                    .Where(x => recordDateSelector(x) >= dayStart && recordDateSelector(x) < dayEnd)
                    .Select(valueSelector)
                    .Where(x => x.HasValue)
                    .Select(x => x!.Value)
                    .ToList();

                points.Add(new HealthDashboardTrendPointResponse
                {
                    Date = TimeHelper.ToTaiwanTime(dayStart).ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
                    Value = dayValues.Count == 0 ? null : Math.Round(dayValues.Average(), 1, MidpointRounding.AwayFromZero)
                });
            }

            return points;
        }

        private static string ResolveBloodPressureLabel(string? preferredLabel, IReadOnlyList<BloodPressureRecord> records)
        {
            if (!string.IsNullOrWhiteSpace(preferredLabel))
            {
                return preferredLabel;
            }

            if (records.Count == 0)
            {
                return "建議觀察";
            }

            var averageSystolic = records.Average(x => x.Systolic);
            var averageDiastolic = records.Average(x => x.Diastolic);
            if (averageSystolic <= 130 && averageDiastolic <= 85)
            {
                return "維持良好";
            }

            var ordered = records.OrderBy(x => x.RecordDate).ToList();
            if (ordered[^1].Systolic <= ordered[0].Systolic && ordered[^1].Diastolic <= ordered[0].Diastolic)
            {
                return "逐步改善";
            }

            return "建議觀察";
        }

        private static string ResolveBloodOxygenLabel(string? preferredLabel, IReadOnlyList<decimal> values)
        {
            if (!string.IsNullOrWhiteSpace(preferredLabel))
            {
                return preferredLabel;
            }

            if (values.Count == 0)
            {
                return "建議觀察";
            }

            if (values.Average() >= 95m)
            {
                return "維持良好";
            }

            return values[^1] >= values[0] ? "逐步改善" : "建議觀察";
        }

        private static string ResolveBloodSugarLabel(string? preferredLabel, IReadOnlyList<decimal> values)
        {
            if (!string.IsNullOrWhiteSpace(preferredLabel))
            {
                return preferredLabel;
            }

            if (values.Count == 0)
            {
                return "建議觀察";
            }

            if (values.Average() <= 140m)
            {
                return "維持良好";
            }

            return values[^1] <= values[0] ? "逐步改善" : "建議觀察";
        }

        private static string ResolveTemperatureLabel(string? preferredLabel, IReadOnlyList<decimal> values)
        {
            if (!string.IsNullOrWhiteSpace(preferredLabel))
            {
                return preferredLabel;
            }

            if (values.Count == 0)
            {
                return "建議觀察";
            }

            var average = values.Average();
            if (average >= 36.0m && average <= 37.5m)
            {
                return "趨於穩定";
            }

            return Math.Abs(values[^1] - 36.8m) < Math.Abs(values[0] - 36.8m)
                ? "逐步改善"
                : "建議觀察";
        }

        private static string ResolveWeightLabel(string? preferredLabel, IReadOnlyList<decimal> values)
        {
            if (!string.IsNullOrWhiteSpace(preferredLabel))
            {
                return preferredLabel;
            }

            if (values.Count == 0)
            {
                return "建議觀察";
            }

            var range = values.Max() - values.Min();
            if (range <= 1m)
            {
                return "維持良好";
            }

            return Math.Abs(values[^1] - values.Average()) <= 0.5m
                ? "趨於穩定"
                : "建議觀察";
        }

        private static AiGeneratedInsightDto MapInsight(AiHealthInsight insight)
        {
            TrendLabelsDto? trendLabels = null;
            if (!string.IsNullOrWhiteSpace(insight.TrendLabels))
            {
                try
                {
                    trendLabels = JsonSerializer.Deserialize<TrendLabelsDto>(insight.TrendLabels, JsonOptions);
                }
                catch
                {
                }
            }

            return new AiGeneratedInsightDto
            {
                OverallSummary = insight.OverallSummary,
                TodaySummary = insight.TodaySummary,
                KeyInsights = insight.KeyInsights,
                Recommendations = insight.Recommendations,
                TrendLabels = trendLabels,
                SourceDataHash = insight.SourceDataHash,
                ModelName = insight.ModelName,
                PromptVersion = insight.PromptVersion,
                GeneratedAt = insight.GeneratedAt
            };
        }

        private static DateTime NormalizeTimestamp(DateTime value)
        {
            var utcValue = value.Kind == DateTimeKind.Utc ? value : value.ToUniversalTime();
            return new DateTime(utcValue.Ticks - (utcValue.Ticks % TimeSpan.TicksPerMillisecond), DateTimeKind.Utc);
        }

        private sealed class DashboardContext
        {
            public DateTime DateFrom { get; init; }
            public DateTime DateTo { get; init; }
            public AiGeneratedInsightDto? Insight { get; init; }
            public bool IsFromCache { get; init; }
            public int TotalRecordCount { get; init; }
            public int TodayRecordCount { get; init; }
            public DateTime? LatestRecordAt { get; init; }
            public IReadOnlyList<string> LatestTodayMetrics { get; init; } = Array.Empty<string>();
            public IReadOnlyList<BloodPressureRecord> BloodPressures { get; init; } = Array.Empty<BloodPressureRecord>();
            public IReadOnlyList<BloodSugarRecord> BloodSugars { get; init; } = Array.Empty<BloodSugarRecord>();
            public IReadOnlyList<WeightRecord> Weights { get; init; } = Array.Empty<WeightRecord>();
            public IReadOnlyList<TemperatureRecord> Temperatures { get; init; } = Array.Empty<TemperatureRecord>();
            public IReadOnlyList<BloodOxygenRecord> BloodOxygens { get; init; } = Array.Empty<BloodOxygenRecord>();
        }

        private sealed class TodayMetricsResult
        {
            public int RecordCount { get; init; }
            public DateTime? LatestRecordAt { get; init; }
            public IReadOnlyList<string> LatestMetrics { get; init; } = Array.Empty<string>();
        }
    }
}
