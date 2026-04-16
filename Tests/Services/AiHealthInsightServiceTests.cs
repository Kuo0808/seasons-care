using SeasonsCare.Api.DTOs.AiHealthInsights;
using SeasonsCare.Api.Exceptions;
using SeasonsCare.Api.Models.Entities;
using SeasonsCare.Api.Repositories;
using SeasonsCare.Api.Services;

namespace SeasonsCare.Api.Tests.Services;

public class AiHealthInsightServiceTests
{
    [Fact]
    public async Task SaveInsightAsync_ThrowsForbidden_WhenUserIsNotMember()
    {
        var repository = new FakeAiHealthInsightRepository();
        var careGroupRepository = new FakeCareGroupRepository(isMember: false);
        var service = new AiHealthInsightService(repository, careGroupRepository);

        var exception = await Assert.ThrowsAsync<DomainException>(() =>
            service.SaveInsightAsync(Guid.NewGuid(), Guid.NewGuid(), new SaveAiHealthInsightRequest
            {
                ReportType = "daily",
                DateFrom = new DateTime(2026, 4, 1),
                DateTo = new DateTime(2026, 4, 1, 23, 59, 59),
                OverallSummary = "summary",
                KeyInsights = "insights",
                Recommendations = "recommendations"
            }));

        Assert.Equal(403, exception.StatusCode);
        Assert.Equal("FORBIDDEN", exception.ErrorCode);
    }

    [Fact]
    public async Task SaveInsightAsync_UpsertsExistingInsight_WhenUniqueKeyMatches()
    {
        var careGroupId = Guid.NewGuid();
        var existing = new AiHealthInsight
        {
            Id = Guid.NewGuid(),
            CareGroupId = careGroupId,
            ReportType = "daily",
            DateFrom = new DateTime(2026, 4, 1, 0, 0, 0, DateTimeKind.Utc),
            DateTo = new DateTime(2026, 4, 1, 23, 59, 59, DateTimeKind.Utc),
            OverallSummary = "old",
            KeyInsights = "old",
            Recommendations = "old",
            GeneratedAt = new DateTime(2026, 4, 1, 8, 0, 0, DateTimeKind.Utc),
            CreatedAt = new DateTime(2026, 4, 1, 8, 0, 0, DateTimeKind.Utc),
            UpdatedAt = new DateTime(2026, 4, 1, 8, 0, 0, DateTimeKind.Utc)
        };

        var repository = new FakeAiHealthInsightRepository(existing);
        var careGroupRepository = new FakeCareGroupRepository(isMember: true);
        var service = new AiHealthInsightService(repository, careGroupRepository);

        var result = await service.SaveInsightAsync(Guid.NewGuid(), careGroupId, new SaveAiHealthInsightRequest
        {
            ReportType = "daily",
            DateFrom = existing.DateFrom,
            DateTo = existing.DateTo,
            OverallSummary = "new",
            KeyInsights = "new insights",
            Recommendations = "new recommendations"
        });

        Assert.Single(repository.Items);
        Assert.Equal(existing.Id, result.Id);
        Assert.Equal("new", repository.Items[0].OverallSummary);
        Assert.Equal("new insights", repository.Items[0].KeyInsights);
    }

    private sealed class FakeAiHealthInsightRepository : IAiHealthInsightRepository
    {
        public List<AiHealthInsight> Items { get; } = new();

        public FakeAiHealthInsightRepository(params AiHealthInsight[] items)
        {
            Items.AddRange(items);
        }

        public Task<AiHealthInsight?> GetByUniqueKeyAsync(Guid careGroupId, string reportType, DateTime dateFrom, DateTime dateTo)
        {
            return Task.FromResult(Items.FirstOrDefault(x =>
                x.CareGroupId == careGroupId &&
                x.ReportType == reportType &&
                x.DateFrom == dateFrom &&
                x.DateTo == dateTo &&
                x.DeletedAt == null));
        }

        public Task<AiHealthInsight?> GetLatestAsync(Guid careGroupId, string? reportType)
        {
            var query = Items.Where(x => x.CareGroupId == careGroupId && x.DeletedAt == null);
            if (!string.IsNullOrWhiteSpace(reportType))
            {
                query = query.Where(x => x.ReportType == reportType);
            }

            return Task.FromResult(query.OrderByDescending(x => x.GeneratedAt).FirstOrDefault());
        }

        public Task<(System.Collections.Generic.IReadOnlyList<AiHealthInsight> Items, int TotalCount)> GetPagedHistoryAsync(Guid careGroupId, string reportType, int page, int pageSize)
        {
            var query = Items.Where(x => x.CareGroupId == careGroupId && x.ReportType == reportType && x.DeletedAt == null);
            var totalCount = query.Count();
            var result = query
                .OrderByDescending(x => x.GeneratedAt)
                .ThenByDescending(x => x.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToList();
            
            return Task.FromResult(((System.Collections.Generic.IReadOnlyList<AiHealthInsight>)result, totalCount));
        }

        public Task AddAsync(AiHealthInsight insight)
        {
            Items.Add(insight);
            return Task.CompletedTask;
        }

        public Task UpdateAsync(AiHealthInsight insight)
        {
            return Task.CompletedTask;
        }

        public Task SaveChangesAsync()
        {
            return Task.CompletedTask;
        }
    }

    private sealed class FakeCareGroupRepository : ICareGroupRepository
    {
        private readonly bool _isMember;

        public FakeCareGroupRepository(bool isMember)
        {
            _isMember = isMember;
        }

        public Task<CareGroup?> GetByIdAsync(Guid id) => Task.FromResult<CareGroup?>(null);
        public Task<CareGroup?> GetByInviteCodeAsync(string inviteCode) => Task.FromResult<CareGroup?>(null);
        public Task<(List<CareGroup> Data, int TotalCount)> GetPagedByUserIdAsync(Guid userId, int page, int pageSize, string sort) => Task.FromResult((new List<CareGroup>(), 0));
        public Task<List<Guid>> GetAccessibleCareGroupIdsAsync(Guid userId) => Task.FromResult(new List<Guid>());
        public Task AddAsync(CareGroup careGroup) => Task.CompletedTask;
        public Task AddMemberAsync(CareGroupMember member) => Task.CompletedTask;
        public Task<bool> IsMemberAsync(Guid careGroupId, Guid userId) => Task.FromResult(_isMember);
        public Task<CareGroupMember?> GetMemberAsync(Guid careGroupId, Guid userId) => Task.FromResult<CareGroupMember?>(null);
        public Task<CareGroupMember?> GetMemberIncludingDeletedAsync(Guid careGroupId, Guid userId) => Task.FromResult<CareGroupMember?>(null);
        public Task<List<CareGroupMember>> GetActiveMembersWithUserAsync(Guid careGroupId) => Task.FromResult(new List<CareGroupMember>());
        public Task SaveChangesAsync() => Task.CompletedTask;
    }
}
