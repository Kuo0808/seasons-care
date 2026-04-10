using SeasonsCare.Api.Models.Entities;
using SeasonsCare.Api.Models.Entities.HealthRecords;
using SeasonsCare.Api.Models.Enums;

namespace SeasonsCare.Api.Tests.Shared;

public static class SeedDataHelper
{
    public static User CreateUser(Guid? userId = null)
    {
        var resolvedUserId = userId ?? TestUsers.DefaultUserId;

        return new User
        {
            Id = resolvedUserId,
            Email = $"test-{resolvedUserId:N}@example.com",
            PasswordHash = "hashed",
            Username = "tester",
            AvatarKey = "dog_01",
            CreatedBy = "test"
        };
    }

    public static CareGroup CreateCareGroup(string name)
    {
        return new CareGroup
        {
            Id = Guid.NewGuid(),
            Name = name,
            RecipientName = $"{name} Recipient",
            RecipientGender = "Unknown",
            RecipientBirthDate = new DateOnly(1950, 1, 2),
            InviteCode = name.Replace(" ", string.Empty).ToUpperInvariant(),
            CreatedBy = "test"
        };
    }

    public static CareGroupMember CreateMember(Guid careGroupId, Guid? userId = null, CareGroupRole role = CareGroupRole.Admin)
    {
        return new CareGroupMember
        {
            Id = Guid.NewGuid(),
            CareGroupId = careGroupId,
            UserId = userId ?? TestUsers.DefaultUserId,
            Role = role,
            CreatedBy = "test"
        };
    }

    public static CareLog CreateCareLog(Guid careGroupId, string title, DateTime? timestamp = null)
    {
        var now = timestamp ?? new DateTime(2026, 3, 20, 2, 0, 0, DateTimeKind.Utc);
        return new CareLog
        {
            Id = Guid.NewGuid(),
            CareGroupId = careGroupId,
            Title = title,
            Description = $"description-{title}",
            StartsAt = now,
            RepeatPattern = "none",
            Participants = new[] { TestUsers.DefaultUserId.ToString() },
            Status = "scheduled",
            IsImportant = false,
            CreatedAt = now,
            UpdatedAt = now,
            CreatedBy = "test"
        };
    }

    public static ExpenseRecord CreateExpense(Guid careGroupId, string title, decimal amount = 100m, DateTime? timestamp = null)
    {
        var now = timestamp ?? new DateTime(2026, 3, 20, 2, 0, 0, DateTimeKind.Utc);
        return new ExpenseRecord
        {
            Id = Guid.NewGuid(),
            CareGroupId = careGroupId,
            Title = title,
            Amount = amount,
            Category = "food",
            Notes = $"note-{title}",
            ExpenseDate = now,
            SplitStatus = ExpenseSplitStatus.None,
            CreatedAt = now,
            UpdatedAt = now,
            CreatedBy = "test"
        };
    }

    public static BloodSugarRecord CreateBloodSugar(Guid careGroupId, decimal glucoseLevel = 120m, string measurementContext = "飯前", DateTime? timestamp = null)
    {
        var now = timestamp ?? new DateTime(2026, 3, 20, 2, 0, 0, DateTimeKind.Utc);
        return new BloodSugarRecord
        {
            Id = Guid.NewGuid(),
            CareGroupId = careGroupId,
            GlucoseLevel = glucoseLevel,
            MeasurementContext = measurementContext,
            Notes = $"note-{measurementContext}",
            RecordDate = now,
            CreatedAt = now,
            UpdatedAt = now,
            CreatedBy = TestUsers.DefaultUserId.ToString()
        };
    }

    public static BloodPressureRecord CreateBloodPressure(Guid careGroupId, int systolic = 120, int diastolic = 80, DateTime? timestamp = null)
    {
        var now = timestamp ?? new DateTime(2026, 3, 20, 2, 0, 0, DateTimeKind.Utc);
        return new BloodPressureRecord
        {
            Id = Guid.NewGuid(),
            CareGroupId = careGroupId,
            Systolic = systolic,
            Diastolic = diastolic,
            Notes = "stable",
            RecordDate = now,
            CreatedAt = now,
            UpdatedAt = now,
            CreatedBy = TestUsers.DefaultUserId.ToString()
        };
    }

    public static BloodOxygenRecord CreateBloodOxygen(Guid careGroupId, decimal spO2 = 98m, DateTime? timestamp = null)
    {
        var now = timestamp ?? new DateTime(2026, 3, 20, 2, 0, 0, DateTimeKind.Utc);
        return new BloodOxygenRecord
        {
            Id = Guid.NewGuid(),
            CareGroupId = careGroupId,
            SpO2 = spO2,
            Notes = "stable",
            RecordDate = now,
            CreatedAt = now,
            UpdatedAt = now,
            CreatedBy = TestUsers.DefaultUserId.ToString()
        };
    }

    public static AiHealthInsight CreateAiHealthInsight(Guid careGroupId, string reportType = "daily", DateTime? generatedAt = null)
    {
        var now = generatedAt ?? new DateTime(2026, 4, 9, 8, 0, 0, DateTimeKind.Utc);
        return new AiHealthInsight
        {
            Id = Guid.NewGuid(),
            CareGroupId = careGroupId,
            ReportType = reportType,
            DateFrom = now.Date,
            DateTo = now.Date.AddDays(1).AddTicks(-1),
            OverallSummary = "Summary",
            KeyInsights = "Insights",
            Recommendations = "Recommendations",
            SourceDataHash = "hash",
            ModelName = "gpt-test",
            PromptVersion = "v1",
            GeneratedAt = now,
            CreatedAt = now,
            UpdatedAt = now,
            CreatedBy = TestUsers.DefaultUserId.ToString()
        };
    }

    public static TemperatureRecord CreateTemperature(Guid careGroupId, decimal value = 36.5m, DateTime? timestamp = null)
    {
        var now = timestamp ?? new DateTime(2026, 3, 20, 2, 0, 0, DateTimeKind.Utc);
        return new TemperatureRecord
        {
            Id = Guid.NewGuid(),
            CareGroupId = careGroupId,
            Value = value,
            Notes = "stable",
            RecordDate = now,
            CreatedAt = now,
            UpdatedAt = now,
            CreatedBy = TestUsers.DefaultUserId.ToString()
        };
    }

    public static WeightRecord CreateWeight(Guid careGroupId, decimal value = 60m, DateTime? timestamp = null)
    {
        var now = timestamp ?? new DateTime(2026, 3, 20, 2, 0, 0, DateTimeKind.Utc);
        return new WeightRecord
        {
            Id = Guid.NewGuid(),
            CareGroupId = careGroupId,
            Value = value,
            Notes = "stable",
            RecordDate = now,
            CreatedAt = now,
            UpdatedAt = now,
            CreatedBy = TestUsers.DefaultUserId.ToString()
        };
    }
}
