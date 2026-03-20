using SeasonsCare.Api.Models.Entities;
using SeasonsCare.Api.Models.Enums;

namespace SeasonsCare.Api.Tests.Shared;

public static class SeedDataHelper
{
    public static User CreateUser(Guid? userId = null)
    {
        return new User
        {
            Id = userId ?? TestUsers.DefaultUserId,
            Email = "test@example.com",
            PasswordHash = "hashed",
            Username = "tester",
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
            Content = $"content-{title}",
            LogType = "Daily",
            RecordDate = now,
            CreatedAt = now,
            UpdatedAt = now,
            CreatedBy = "test"
        };
    }
}
