using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
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

        public async Task<HealthDashboardResponse> GetDashboardAsync(Guid currentUserId, Guid careGroupId)
        {
            var isMember = await _careGroupRepository.IsMemberAsync(careGroupId, currentUserId);
            if (!isMember)
            {
                throw new DomainException("You are not a member of this care group.", "FORBIDDEN", 403);
            }

            var todayStart = NormalizeTimestamp(DateTime.UtcNow.Date);
            var dateFrom = NormalizeTimestamp(todayStart.AddDays(-6));
            var dateTo = NormalizeTimestamp(todayStart.AddDays(1).AddMilliseconds(-1));

            var bloodPressures = await _bloodPressureRepository.GetByCareGroupIdAndDateRangeAsync(careGroupId, dateFrom, dateTo);
            var bloodSugars = await _bloodSugarRepository.GetByCareGroupIdAndDateRangeAsync(careGroupId, dateFrom, dateTo);
            var weights = await _weightRepository.GetByCareGroupIdAndDateRangeAsync(careGroupId, dateFrom, dateTo);
            var temperatures = await _temperatureRepository.GetByCareGroupIdAndDateRangeAsync(careGroupId, dateFrom, dateTo);
            var bloodOxygens = await _bloodOxygenRepository.GetByCareGroupIdAndDateRangeAsync(careGroupId, dateFrom, dateTo);

            var trends = BuildTrends(bloodPressures, bloodSugars, weights, temperatures, bloodOxygens);
            var todaySummary = BuildTodaySummary(todayStart, bloodPressures, bloodSugars, weights, temperatures, bloodOxygens);

            var cachedInsight = await _aiHealthInsightRepository.GetByUniqueKeyAsync(careGroupId, DashboardReportType, dateFrom, dateTo);
            if (cachedInsight != null)
            {
                if (!string.IsNullOrWhiteSpace(cachedInsight.TodaySummary))
                {
                    todaySummary.SummaryText = cachedInsight.TodaySummary;
                }

                return new HealthDashboardResponse
                {
                    AiReport = MapInsight(cachedInsight),
                    TodaySummary = todaySummary,
                    Trends = trends,
                    DateFrom = dateFrom,
                    DateTo = dateTo,
                    IsFromCache = true
                };
            }

            AiGeneratedInsightDto? generatedInsight = null;

            try
            {
                var promptInput = BuildPromptInput(careGroupId, dateFrom, dateTo, todaySummary, bloodPressures, bloodSugars, weights, temperatures, bloodOxygens);
                generatedInsight = await _aiIntegrationService.GenerateHealthInsightAsync(promptInput);

                var savedInsight = await _aiHealthInsightService.SaveInsightAsync(currentUserId, careGroupId, new SaveAiHealthInsightRequest
                {
                    ReportType = DashboardReportType,
                    DateFrom = dateFrom,
                    DateTo = dateTo,
                    OverallSummary = generatedInsight.OverallSummary,
                    TodaySummary = generatedInsight.TodaySummary,
                    KeyInsights = generatedInsight.KeyInsights,
                    Recommendations = generatedInsight.Recommendations,
                    SourceDataHash = generatedInsight.SourceDataHash,
                    ModelName = generatedInsight.ModelName,
                    PromptVersion = generatedInsight.PromptVersion
                });

                generatedInsight = new AiGeneratedInsightDto
                {
                    OverallSummary = savedInsight.OverallSummary,
                    TodaySummary = savedInsight.TodaySummary,
                    KeyInsights = savedInsight.KeyInsights,
                    Recommendations = savedInsight.Recommendations,
                    SourceDataHash = savedInsight.SourceDataHash,
                    ModelName = savedInsight.ModelName,
                    PromptVersion = savedInsight.PromptVersion,
                    GeneratedAt = savedInsight.GeneratedAt
                };
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to generate health dashboard AI insight for careGroupId {CareGroupId}. Returning dashboard without AI report.", careGroupId);
            }

            if (generatedInsight != null && !string.IsNullOrWhiteSpace(generatedInsight.TodaySummary))
            {
                todaySummary.SummaryText = generatedInsight.TodaySummary;
            }

            return new HealthDashboardResponse
            {
                AiReport = generatedInsight,
                TodaySummary = todaySummary,
                Trends = trends,
                DateFrom = dateFrom,
                DateTo = dateTo,
                IsFromCache = false
            };
        }

        private static HealthDashboardTrendsDto BuildTrends(
            IEnumerable<BloodPressureRecord> bloodPressures,
            IEnumerable<BloodSugarRecord> bloodSugars,
            IEnumerable<WeightRecord> weights,
            IEnumerable<TemperatureRecord> temperatures,
            IEnumerable<BloodOxygenRecord> bloodOxygens)
        {
            return new HealthDashboardTrendsDto
            {
                BloodPressures = bloodPressures
                    .OrderBy(x => x.RecordDate)
                    .Select(x => new BloodPressureTrendPointDto
                    {
                        RecordDate = x.RecordDate,
                        Systolic = x.Systolic,
                        Diastolic = x.Diastolic,
                        Notes = x.Notes
                    })
                    .ToList(),
                BloodSugars = bloodSugars
                    .OrderBy(x => x.RecordDate)
                    .Select(x => new BloodSugarTrendPointDto
                    {
                        RecordDate = x.RecordDate,
                        Value = x.GlucoseLevel,
                        MeasurementContext = x.MeasurementContext,
                        Notes = x.Notes
                    })
                    .ToList(),
                Weights = weights
                    .OrderBy(x => x.RecordDate)
                    .Select(x => new SingleValueTrendPointDto
                    {
                        RecordDate = x.RecordDate,
                        Value = x.Value,
                        Notes = x.Notes
                    })
                    .ToList(),
                Temperatures = temperatures
                    .OrderBy(x => x.RecordDate)
                    .Select(x => new SingleValueTrendPointDto
                    {
                        RecordDate = x.RecordDate,
                        Value = x.Value,
                        Notes = x.Notes
                    })
                    .ToList(),
                BloodOxygens = bloodOxygens
                    .OrderBy(x => x.RecordDate)
                    .Select(x => new SingleValueTrendPointDto
                    {
                        RecordDate = x.RecordDate,
                        Value = x.SpO2,
                        Notes = x.Notes
                    })
                    .ToList()
            };
        }

        private static HealthDashboardTodaySummaryDto BuildTodaySummary(
            DateTime todayStart,
            IEnumerable<BloodPressureRecord> bloodPressures,
            IEnumerable<BloodSugarRecord> bloodSugars,
            IEnumerable<WeightRecord> weights,
            IEnumerable<TemperatureRecord> temperatures,
            IEnumerable<BloodOxygenRecord> bloodOxygens)
        {
            var todayEndExclusive = todayStart.AddDays(1);
            var todayBloodPressures = bloodPressures.Where(x => x.RecordDate >= todayStart && x.RecordDate < todayEndExclusive).ToList();
            var todayBloodSugars = bloodSugars.Where(x => x.RecordDate >= todayStart && x.RecordDate < todayEndExclusive).ToList();
            var todayWeights = weights.Where(x => x.RecordDate >= todayStart && x.RecordDate < todayEndExclusive).ToList();
            var todayTemperatures = temperatures.Where(x => x.RecordDate >= todayStart && x.RecordDate < todayEndExclusive).ToList();
            var todayBloodOxygens = bloodOxygens.Where(x => x.RecordDate >= todayStart && x.RecordDate < todayEndExclusive).ToList();

            var allTodayTimestamps = todayBloodPressures.Select(x => x.RecordDate)
                .Concat(todayBloodSugars.Select(x => x.RecordDate))
                .Concat(todayWeights.Select(x => x.RecordDate))
                .Concat(todayTemperatures.Select(x => x.RecordDate))
                .Concat(todayBloodOxygens.Select(x => x.RecordDate))
                .ToList();

            var parts = new List<string>();
            if (todayBloodPressures.Count > 0)
            {
                var latest = todayBloodPressures.OrderByDescending(x => x.RecordDate).First();
                parts.Add($"血壓 {todayBloodPressures.Count} 筆，最新 {latest.Systolic}/{latest.Diastolic} mmHg");
            }

            if (todayBloodSugars.Count > 0)
            {
                var latest = todayBloodSugars.OrderByDescending(x => x.RecordDate).First();
                parts.Add($"血糖 {todayBloodSugars.Count} 筆，最新 {latest.GlucoseLevel.ToString("0.##", CultureInfo.InvariantCulture)} mg/dL");
            }

            if (todayWeights.Count > 0)
            {
                var latest = todayWeights.OrderByDescending(x => x.RecordDate).First();
                parts.Add($"體重 {todayWeights.Count} 筆，最新 {latest.Value.ToString("0.##", CultureInfo.InvariantCulture)} kg");
            }

            if (todayTemperatures.Count > 0)
            {
                var latest = todayTemperatures.OrderByDescending(x => x.RecordDate).First();
                parts.Add($"體溫 {todayTemperatures.Count} 筆，最新 {latest.Value.ToString("0.##", CultureInfo.InvariantCulture)} °C");
            }

            if (todayBloodOxygens.Count > 0)
            {
                var latest = todayBloodOxygens.OrderByDescending(x => x.RecordDate).First();
                parts.Add($"血氧 {todayBloodOxygens.Count} 筆，最新 {latest.SpO2.ToString("0.##", CultureInfo.InvariantCulture)}%");
            }

            return new HealthDashboardTodaySummaryDto
            {
                SummaryText = parts.Count > 0
                    ? $"今日共新增 {allTodayTimestamps.Count} 筆健康紀錄：" + string.Join("；", parts)
                    : "今日尚無新的健康紀錄。",
                RecordCount = allTodayTimestamps.Count,
                LatestRecordAt = allTodayTimestamps.Count > 0 ? allTodayTimestamps.Max() : null
            };
        }

        private static HealthInsightPromptInput BuildPromptInput(
            Guid careGroupId,
            DateTime dateFrom,
            DateTime dateTo,
            HealthDashboardTodaySummaryDto todaySummary,
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
                TodaySummary = todaySummary.SummaryText,
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
                return "近 7 天沒有血壓資料。";
            }

            var latest = list[^1];
            var avgSystolic = list.Average(x => x.Systolic);
            var avgDiastolic = list.Average(x => x.Diastolic);
            var minSystolic = list.Min(x => x.Systolic);
            var maxSystolic = list.Max(x => x.Systolic);
            var minDiastolic = list.Min(x => x.Diastolic);
            var maxDiastolic = list.Max(x => x.Diastolic);

            return $"共 {list.Count} 筆；最新 {latest.Systolic}/{latest.Diastolic} mmHg；平均 {avgSystolic:0.#}/{avgDiastolic:0.#} mmHg；收縮壓範圍 {minSystolic}-{maxSystolic}；舒張壓範圍 {minDiastolic}-{maxDiastolic}；趨勢 {DescribeTrend(list.Select(x => (double)x.Systolic))}。";
        }

        private static string BuildBloodSugarSummary(IEnumerable<BloodSugarRecord> records)
        {
            var list = records.OrderBy(x => x.RecordDate).ToList();
            if (list.Count == 0)
            {
                return "近 7 天沒有血糖資料。";
            }

            var latest = list[^1];
            var groupedContexts = list
                .GroupBy(x => x.MeasurementContext)
                .Select(x => $"{x.Key}:{x.Count()} 筆")
                .ToList();

            return $"共 {list.Count} 筆；最新 {latest.GlucoseLevel:0.##} mg/dL；平均 {list.Average(x => x.GlucoseLevel):0.##} mg/dL；範圍 {list.Min(x => x.GlucoseLevel):0.##}-{list.Max(x => x.GlucoseLevel):0.##} mg/dL；量測情境 {string.Join("、", groupedContexts)}；趨勢 {DescribeTrend(list.Select(x => (double)x.GlucoseLevel))}。";
        }

        private static string BuildSingleMetricSummary(IEnumerable<double> values, string metricName, string unit)
        {
            var list = values.ToList();
            if (list.Count == 0)
            {
                return $"近 7 天沒有{metricName}資料。";
            }

            return $"共 {list.Count} 筆；最新 {list[^1]:0.##}{unit}；平均 {list.Average():0.##}{unit}；範圍 {list.Min():0.##}-{list.Max():0.##}{unit}；趨勢 {DescribeTrend(list)}。";
        }

        private static string DescribeTrend(IEnumerable<double> values)
        {
            var list = values.ToList();
            if (list.Count < 2)
            {
                return "資料不足";
            }

            var delta = list[^1] - list[0];
            if (Math.Abs(delta) < 0.01)
            {
                return "大致持平";
            }

            return delta > 0 ? "整體上升" : "整體下降";
        }

        private static AiGeneratedInsightDto MapInsight(AiHealthInsight insight)
        {
            return new AiGeneratedInsightDto
            {
                OverallSummary = insight.OverallSummary,
                TodaySummary = insight.TodaySummary,
                KeyInsights = insight.KeyInsights,
                Recommendations = insight.Recommendations,
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
    }
}
