using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http.Json;
using System.Threading.Tasks;
using SeasonsCare.Api.DTOs.Common;
using SeasonsCare.Api.DTOs.Expenses;
using SeasonsCare.Api.Exceptions;
using SeasonsCare.Api.Models.Enums;
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
            amount = 100,
            category = "food",
            expenseDate = "2026-04-10T00:00:00Z",
            splitStatus = "none"
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
            amount = 120,
            category = "traffic",
            expenseDate = "2026-04-10T00:00:00Z",
            splitStatus = "pending"
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        var payload = await JsonResponseHelper.ReadJsonAsync(response);
        Assert.Equal("VALIDATION_FAILED", payload.RootElement.GetProperty("errorCode").GetString());
    }

    [Fact]
    public async Task CreateExpense_AcceptsMissingSplitStatus_AndDefaultsToNone()
    {
        using var factory = new StubApiFactory<IExpenseService>(new StubExpenseService());
        using var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/care-groups/aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa/expenses", new
        {
            title = "Groceries",
            amount = 100,
            category = "food",
            expenseDate = "2026-04-10T00:00:00Z"
        });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var payload = await JsonResponseHelper.ReadJsonAsync(response);
        Assert.Equal("none", payload.RootElement.GetProperty("data").GetProperty("splitStatus").GetString());
    }

    [Fact]
    public async Task CreateExpense_ReturnsBadRequest_WhenSplitStatusIsNumeric()
    {
        using var factory = new StubApiFactory<IExpenseService>(new StubExpenseService());
        using var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/care-groups/aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa/expenses", new
        {
            title = "Groceries",
            amount = 100,
            category = "food",
            expenseDate = "2026-04-10T00:00:00Z",
            splitStatus = 1
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
            category = "traffic",
            expenseDate = "2026-04-10T00:00:00Z",
            splitStatus = "pending",
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

    [Fact]
    public async Task GetSplitPreview_ReturnsSuccess_WhenServiceProvidesPreviewData()
    {
        using var factory = new StubApiFactory<IExpenseService>(new StubExpenseService());
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/api/care-groups/aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa/expenses/split-preview?splitMode=daily&targetDate=2026-04-21");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var payload = await JsonResponseHelper.ReadJsonAsync(response);
        var data = payload.RootElement.GetProperty("data");

        Assert.Equal(1, data.GetProperty("expenseCount").GetInt32());
        Assert.Equal(1200m, data.GetProperty("totalAmount").GetDecimal());
        Assert.Equal(1, data.GetProperty("selectedExpenses").GetArrayLength());
        Assert.Equal(2, data.GetProperty("splitDetails").GetArrayLength());
    }

    private sealed class StubExpenseService : IExpenseService
    {
        public Exception? GetExpenseByIdException { get; init; }
        public Exception? UpdateException { get; init; }

        public Task<PagedResponse<ExpenseResponse>> GetExpensesAsync(Guid currentUserId, Guid careGroupId, PaginationRequest pagination)
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
                Amount = 100m,
                Category = "traffic",
                Notes = string.Empty,
                ExpenseDate = new DateTime(2026, 4, 10, 0, 0, 0, DateTimeKind.Utc),
                SplitStatus = ExpenseSplitStatus.None,
                CreatedAt = new DateTime(2026, 4, 10, 0, 0, 0, DateTimeKind.Utc),
                UpdatedAt = new DateTime(2026, 4, 10, 0, 0, 0, DateTimeKind.Utc)
            });
        }

        public Task<ExpenseResponse> CreateExpenseAsync(Guid currentUserId, Guid careGroupId, CreateExpenseRequest request)
        {
            return Task.FromResult(new ExpenseResponse
            {
                Id = Guid.NewGuid(),
                CareGroupId = careGroupId,
                Title = request.Title,
                Amount = request.Amount,
                Category = request.Category,
                Notes = request.Notes ?? string.Empty,
                ExpenseDate = request.ExpenseDate,
                SplitStatus = request.SplitStatus,
                CreatedAt = request.ExpenseDate,
                UpdatedAt = request.ExpenseDate
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
                Category = request.Category,
                Notes = request.Notes ?? string.Empty,
                ExpenseDate = request.ExpenseDate,
                SplitStatus = request.SplitStatus,
                CreatedAt = request.ExpenseDate,
                UpdatedAt = request.UpdatedAt ?? request.ExpenseDate
            });
        }

        public Task DeleteExpenseAsync(Guid currentUserId, Guid careGroupId, Guid expenseId)
        {
            return Task.CompletedTask;
        }

        public Task<ExpenseSplitPreviewResponse> GetSplitPreviewAsync(Guid currentUserId, Guid careGroupId, SplitPreviewQueryRequest request)
        {
            return Task.FromResult(new ExpenseSplitPreviewResponse
            {
                ExpenseCount = 1,
                TotalAmount = 1200m,
                SelectedExpenses = new List<ExpenseItemSummary>
                {
                    new ExpenseItemSummary
                    {
                        Id = Guid.NewGuid(),
                        Title = "回診費",
                        Amount = 1200m
                    }
                },
                SplitDetails = new List<SplitUserDetail>
                {
                    new SplitUserDetail
                    {
                        UserId = Guid.NewGuid(),
                        Name = "王希銘",
                        IsPayer = true,
                        ReceivableAmount = 600m,
                        PayableAmount = 0m
                    },
                    new SplitUserDetail
                    {
                        UserId = Guid.NewGuid(),
                        Name = "王小美",
                        IsPayer = false,
                        ReceivableAmount = 0m,
                        PayableAmount = 600m
                    }
                }
            });
        }

        public Task<ExpenseSplitPreviewResponse> PreviewSplitAsync(Guid currentUserId, Guid careGroupId, SplitPreviewRequest request)
        {
            throw new NotImplementedException();
        }

        public Task ConfirmSplitAsync(Guid currentUserId, Guid careGroupId, SplitConfirmRequest request)
        {
            throw new NotImplementedException();
        }

        public Task<MemberExpenseTotalsResponse> GetMemberExpenseTotalsAsync(Guid currentUserId, Guid careGroupId, MemberExpenseTotalsRequest request)
        {
            throw new NotImplementedException();
        }
    }
}
