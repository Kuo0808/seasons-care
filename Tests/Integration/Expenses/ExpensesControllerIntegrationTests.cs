using System.Net;
using System.Net.Http.Json;
using SeasonsCare.Api.DTOs.Expenses;
using SeasonsCare.Api.Exceptions;
using SeasonsCare.Api.Services;
using SeasonsCare.Api.Tests.Shared;
using SeasonsCare.Api.Tests.Shared.Http;

namespace SeasonsCare.Api.Tests.Integration.Expenses;

public class ExpensesControllerIntegrationTests
{
    [Fact]
    public async Task CreateExpense_ReturnsBadRequest_WhenTitleIsMissing()
    {
        using var factory = new StubApiFactory<IExpenseService>(new StubExpenseService());
        using var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/care-groups/aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa/expenses", new
        {
            title = "",
            amount = 100
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        var payload = await JsonResponseHelper.ReadJsonAsync(response);
        Assert.Equal("VALIDATION_FAILED", payload.RootElement.GetProperty("errorCode").GetString());
    }

    [Fact]
    public async Task UpdateExpense_ReturnsBadRequest_WhenUpdatedAtIsMissing()
    {
        using var factory = new StubApiFactory<IExpenseService>(new StubExpenseService());
        using var client = factory.CreateClient();

        var response = await client.PutAsJsonAsync("/api/care-groups/aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa/expenses/bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb", new
        {
            title = "Taxi",
            amount = 120
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        var payload = await JsonResponseHelper.ReadJsonAsync(response);
        Assert.Equal("VALIDATION_FAILED", payload.RootElement.GetProperty("errorCode").GetString());
    }

    [Fact]
    public async Task GetExpense_ReturnsForbidden_WhenServiceRejectsAccess()
    {
        using var factory = new StubApiFactory<IExpenseService>(new StubExpenseService
        {
            GetExpenseByIdException = new DomainException("forbidden", "FORBIDDEN", 403)
        });
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/api/care-groups/aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa/expenses/bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);

        var payload = await JsonResponseHelper.ReadJsonAsync(response);
        Assert.Equal("FORBIDDEN", payload.RootElement.GetProperty("errorCode").GetString());
    }

    [Fact]
    public async Task UpdateExpense_ReturnsConflict_WhenServiceDetectsConcurrencyConflict()
    {
        using var factory = new StubApiFactory<IExpenseService>(new StubExpenseService
        {
            UpdateException = new DomainException("conflict", "CONCURRENCY_CONFLICT", 409)
        });
        using var client = factory.CreateClient();

        var response = await client.PutAsJsonAsync("/api/care-groups/aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa/expenses/bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb", new
        {
            title = "Taxi",
            amount = 120,
            updatedAt = "2026-03-20T02:00:00Z"
        });

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);

        var payload = await JsonResponseHelper.ReadJsonAsync(response);
        Assert.Equal("CONCURRENCY_CONFLICT", payload.RootElement.GetProperty("errorCode").GetString());
    }

    [Fact]
    public async Task GetExpenses_ReturnsBadRequest_WhenStartDateIsAfterEndDate()
    {
        using var factory = new StubApiFactory<IExpenseService>(new StubExpenseService());
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/api/care-groups/aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa/expenses?startDate=2026-04-10&endDate=2026-04-09");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        var payload = await JsonResponseHelper.ReadJsonAsync(response);
        Assert.Equal("VALIDATION_FAILED", payload.RootElement.GetProperty("errorCode").GetString());
    }

    private sealed class StubExpenseService : IExpenseService
    {
        public Exception? GetExpenseByIdException { get; init; }
        public Exception? UpdateException { get; init; }

        public Task<SeasonsCare.Api.DTOs.Common.PagedResponse<ExpenseResponse>> GetExpensesAsync(Guid currentUserId, Guid careGroupId, SeasonsCare.Api.DTOs.Common.PaginationRequest pagination)
        {
            throw new NotImplementedException();
        }

        public Task<ExpenseResponse> GetExpenseByIdAsync(Guid currentUserId, Guid careGroupId, Guid expenseId)
        {
            if (GetExpenseByIdException is not null)
            {
                throw GetExpenseByIdException;
            }

            return Task.FromResult(new ExpenseResponse
            {
                Id = expenseId,
                CareGroupId = careGroupId,
                Title = "Taxi",
                Amount = 100m
            });
        }

        public Task<ExpenseResponse> CreateExpenseAsync(Guid currentUserId, Guid careGroupId, CreateExpenseRequest request)
        {
            return Task.FromResult(new ExpenseResponse
            {
                Id = Guid.NewGuid(),
                CareGroupId = careGroupId,
                Title = request.Title,
                Amount = request.Amount
            });
        }

        public Task<ExpenseResponse> UpdateExpenseAsync(Guid currentUserId, Guid careGroupId, Guid expenseId, UpdateExpenseRequest request)
        {
            if (UpdateException is not null)
            {
                throw UpdateException;
            }

            return Task.FromResult(new ExpenseResponse
            {
                Id = expenseId,
                CareGroupId = careGroupId,
                Title = request.Title,
                Amount = request.Amount,
                UpdatedAt = request.UpdatedAt
            });
        }

        public Task DeleteExpenseAsync(Guid currentUserId, Guid careGroupId, Guid expenseId)
        {
            return Task.CompletedTask;
        }
    }
}
