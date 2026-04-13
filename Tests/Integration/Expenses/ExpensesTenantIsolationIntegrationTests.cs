using System.Net;
using System.Net.Http.Json;
using SeasonsCare.Api.Models.Entities;
using SeasonsCare.Api.Models.Enums;
using SeasonsCare.Api.Tests.Shared;
using SeasonsCare.Api.Tests.Shared.Http;

namespace SeasonsCare.Api.Tests.Integration.Expenses;

public class ExpensesTenantIsolationIntegrationTests
{
    [Fact]
    public async Task GetExpenses_ReturnsOnlyExpensesFromRequestedCareGroup()
    {
        using var factory = new RealApiFactory();
        using var client = factory.Factory.CreateClient();
        var now = DateTime.UtcNow;
        var currentMonthExpenseDate = new DateTime(now.Year, now.Month, 10, 2, 0, 0, DateTimeKind.Utc);

        var careGroupA = SeedDataHelper.CreateCareGroup("Group A");
        var careGroupB = SeedDataHelper.CreateCareGroup("Group B");
        await factory.SeedAsync(
            SeedDataHelper.CreateUser(),
            careGroupA,
            careGroupB,
            SeedDataHelper.CreateMember(careGroupA.Id),
            SeedDataHelper.CreateMember(careGroupB.Id),
            SeedDataHelper.CreateExpense(careGroupA.Id, "Taxi", 100m, currentMonthExpenseDate),
            SeedDataHelper.CreateExpense(careGroupB.Id, "Snacks", 50m, currentMonthExpenseDate));

        var response = await client.GetAsync($"/api/care-groups/{careGroupA.Id}/expenses");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        using var payload = await JsonResponseHelper.ReadJsonAsync(response);
        var items = payload.RootElement.GetProperty("data");
        Assert.Equal(1, items.GetArrayLength());
        Assert.Equal("Taxi", items[0].GetProperty("title").GetString());
    }

    [Fact]
    public async Task GetExpenses_FiltersByExpenseDateRange()
    {
        using var factory = new RealApiFactory();
        using var client = factory.Factory.CreateClient();

        var careGroup = SeedDataHelper.CreateCareGroup("Group A");
        await factory.SeedAsync(
            SeedDataHelper.CreateUser(),
            careGroup,
            SeedDataHelper.CreateMember(careGroup.Id),
            SeedDataHelper.CreateExpense(careGroup.Id, "Old Expense", 100m, new DateTime(2026, 1, 15, 2, 0, 0, DateTimeKind.Utc)),
            SeedDataHelper.CreateExpense(careGroup.Id, "April Expense", 100m, new DateTime(2026, 4, 15, 2, 0, 0, DateTimeKind.Utc)));

        var response = await client.GetAsync($"/api/care-groups/{careGroup.Id}/expenses?startDate=2026-04-01&endDate=2026-04-30");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        using var payload = await JsonResponseHelper.ReadJsonAsync(response);
        var items = payload.RootElement.GetProperty("data");
        Assert.Equal(1, items.GetArrayLength());
        Assert.Equal("April Expense", items[0].GetProperty("title").GetString());
    }

    [Fact]
    public async Task GetExpenses_WithoutDateRange_ReturnsCurrentMonthExpensesOnly()
    {
        using var factory = new RealApiFactory();
        using var client = factory.Factory.CreateClient();

        var careGroup = SeedDataHelper.CreateCareGroup("Group A");
        var now = DateTime.UtcNow;
        var currentMonthExpenseDate = new DateTime(now.Year, now.Month, 15, 2, 0, 0, DateTimeKind.Utc);
        var previousMonthExpenseDate = currentMonthExpenseDate.AddMonths(-1);
        var nextMonthExpenseDate = currentMonthExpenseDate.AddMonths(1);

        await factory.SeedAsync(
            SeedDataHelper.CreateUser(),
            careGroup,
            SeedDataHelper.CreateMember(careGroup.Id),
            SeedDataHelper.CreateExpense(careGroup.Id, "Previous Month Expense", 100m, previousMonthExpenseDate),
            SeedDataHelper.CreateExpense(careGroup.Id, "Current Month Expense", 200m, currentMonthExpenseDate),
            SeedDataHelper.CreateExpense(careGroup.Id, "Next Month Expense", 300m, nextMonthExpenseDate));

        var response = await client.GetAsync($"/api/care-groups/{careGroup.Id}/expenses");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        using var payload = await JsonResponseHelper.ReadJsonAsync(response);
        var items = payload.RootElement.GetProperty("data");
        var titles = items.EnumerateArray()
            .Select(x => x.GetProperty("title").GetString())
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .ToList();

        Assert.Contains("Current Month Expense", titles);
        Assert.DoesNotContain("Previous Month Expense", titles);
        Assert.DoesNotContain("Next Month Expense", titles);
    }

    [Fact]
    public async Task GetExpenseById_ReturnsNotFound_WhenExpenseIsInDifferentCareGroup()
    {
        using var factory = new RealApiFactory();
        using var client = factory.Factory.CreateClient();

        var careGroupA = SeedDataHelper.CreateCareGroup("Group A");
        var careGroupB = SeedDataHelper.CreateCareGroup("Group B");
        var expenseInGroupB = SeedDataHelper.CreateExpense(careGroupB.Id, "Snacks");

        await factory.SeedAsync(
            SeedDataHelper.CreateUser(),
            careGroupA,
            careGroupB,
            SeedDataHelper.CreateMember(careGroupA.Id),
            SeedDataHelper.CreateMember(careGroupB.Id),
            expenseInGroupB);

        var response = await client.GetAsync($"/api/care-groups/{careGroupA.Id}/expenses/{expenseInGroupB.Id}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);

        using var payload = await JsonResponseHelper.ReadJsonAsync(response);
        Assert.Equal("NOT_FOUND", payload.RootElement.GetProperty("errorCode").GetString());
    }

    [Fact]
    public async Task CreateExpense_PersistsExpense_InRequestedCareGroup()
    {
        using var factory = new RealApiFactory();
        using var client = factory.Factory.CreateClient();
        var expenseDate = DateTime.UtcNow.AddMinutes(-1);

        var careGroup = SeedDataHelper.CreateCareGroup("Group A");
        await factory.SeedAsync(
            SeedDataHelper.CreateUser(),
            careGroup,
            SeedDataHelper.CreateMember(careGroup.Id));

        var response = await client.PostAsJsonAsync($"/api/care-groups/{careGroup.Id}/expenses", new
        {
            title = "Groceries",
            amount = 399.5m,
            category = "food",
            notes = "fruit",
            expenseDate,
            splitStatus = "pending"
        });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        using var payload = await JsonResponseHelper.ReadJsonAsync(response);
        var data = payload.RootElement.GetProperty("data");
        var createdId = data.GetProperty("id").GetGuid();

        var saved = await factory.FindAsync<ExpenseRecord>(createdId);
        Assert.NotNull(saved);
        Assert.Equal(careGroup.Id, saved!.CareGroupId);
        Assert.Equal("Groceries", saved.Title);
        Assert.Equal(399.5m, saved.Amount);
        Assert.Equal(ExpenseSplitStatus.Pending, saved.SplitStatus);
        Assert.NotNull(saved.UpdatedAt);
    }

    [Fact]
    public async Task CreateExpense_DefaultsSplitStatusToNone_WhenOmitted()
    {
        using var factory = new RealApiFactory();
        using var client = factory.Factory.CreateClient();
        var expenseDate = DateTime.UtcNow.AddMinutes(-1);

        var careGroup = SeedDataHelper.CreateCareGroup("Group A");
        await factory.SeedAsync(
            SeedDataHelper.CreateUser(),
            careGroup,
            SeedDataHelper.CreateMember(careGroup.Id));

        var response = await client.PostAsJsonAsync($"/api/care-groups/{careGroup.Id}/expenses", new
        {
            title = "Groceries",
            amount = 399.5m,
            category = "food",
            notes = "fruit",
            expenseDate
        });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        using var payload = await JsonResponseHelper.ReadJsonAsync(response);
        var createdId = payload.RootElement.GetProperty("data").GetProperty("id").GetGuid();

        var saved = await factory.FindAsync<ExpenseRecord>(createdId);
        Assert.NotNull(saved);
        Assert.Equal(ExpenseSplitStatus.None, saved!.SplitStatus);
    }

    [Fact]
    public async Task CreateExpense_RejectsNumericSplitStatus()
    {
        using var factory = new RealApiFactory();
        using var client = factory.Factory.CreateClient();
        var expenseDate = DateTime.UtcNow.AddMinutes(-1);

        var careGroup = SeedDataHelper.CreateCareGroup("Group A");
        await factory.SeedAsync(
            SeedDataHelper.CreateUser(),
            careGroup,
            SeedDataHelper.CreateMember(careGroup.Id));

        var response = await client.PostAsJsonAsync($"/api/care-groups/{careGroup.Id}/expenses", new
        {
            title = "Groceries",
            amount = 399.5m,
            category = "food",
            notes = "fruit",
            expenseDate,
            splitStatus = 1
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        using var payload = await JsonResponseHelper.ReadJsonAsync(response);
        Assert.Equal("VALIDATION_FAILED", payload.RootElement.GetProperty("errorCode").GetString());
    }

    [Fact]
    public async Task UpdateExpense_RefreshesUpdatedAt_AndPersistsChanges()
    {
        using var factory = new RealApiFactory();
        using var client = factory.Factory.CreateClient();
        var updatedExpenseDate = DateTime.UtcNow.AddMinutes(-1);
        var normalizedUpdatedExpenseDate = new DateTime(
            updatedExpenseDate.ToUniversalTime().Ticks - (updatedExpenseDate.ToUniversalTime().Ticks % TimeSpan.TicksPerMillisecond),
            DateTimeKind.Utc);

        var careGroup = SeedDataHelper.CreateCareGroup("Group A");
        var existingExpense = SeedDataHelper.CreateExpense(careGroup.Id, "Taxi", 120m);
        await factory.SeedAsync(
            SeedDataHelper.CreateUser(),
            careGroup,
            SeedDataHelper.CreateMember(careGroup.Id),
            existingExpense);
        var persistedExpense = await factory.FindAsync<ExpenseRecord>(existingExpense.Id);

        var response = await client.PutAsJsonAsync($"/api/care-groups/{careGroup.Id}/expenses/{existingExpense.Id}", new
        {
            title = "Groceries",
            amount = 520m,
            category = "food",
            notes = "weekly shopping",
            expenseDate = updatedExpenseDate,
            splitStatus = "settled",
            updatedAt = persistedExpense!.UpdatedAt!.Value
        });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var saved = await factory.FindAsync<ExpenseRecord>(existingExpense.Id);
        Assert.NotNull(saved);
        Assert.Equal("Groceries", saved!.Title);
        Assert.Equal(520m, saved.Amount);
        Assert.Equal("food", saved.Category);
        Assert.Equal("weekly shopping", saved.Notes);
        Assert.Equal(normalizedUpdatedExpenseDate, saved.ExpenseDate);
        Assert.Equal(ExpenseSplitStatus.Settled, saved.SplitStatus);
        Assert.True(saved.UpdatedAt > persistedExpense.UpdatedAt);
    }

    [Fact]
    public async Task DeleteExpense_SoftDeletesExpense_AndHidesItFromList()
    {
        using var factory = new RealApiFactory();
        using var client = factory.Factory.CreateClient();

        var careGroup = SeedDataHelper.CreateCareGroup("Group A");
        var existingExpense = SeedDataHelper.CreateExpense(careGroup.Id, "Delete Me", 50m);
        await factory.SeedAsync(
            SeedDataHelper.CreateUser(),
            careGroup,
            SeedDataHelper.CreateMember(careGroup.Id),
            existingExpense);

        var deleteResponse = await client.DeleteAsync($"/api/care-groups/{careGroup.Id}/expenses/{existingExpense.Id}");
        Assert.Equal(HttpStatusCode.OK, deleteResponse.StatusCode);

        var allExpenses = await factory.GetExpensesAsync();
        var deleted = allExpenses.Single(x => x.Id == existingExpense.Id);
        Assert.NotNull(deleted.DeletedAt);

        var listResponse = await client.GetAsync($"/api/care-groups/{careGroup.Id}/expenses");
        Assert.Equal(HttpStatusCode.OK, listResponse.StatusCode);

        using var payload = await JsonResponseHelper.ReadJsonAsync(listResponse);
        var items = payload.RootElement.GetProperty("data");
        Assert.Equal(0, items.GetArrayLength());
    }
}
