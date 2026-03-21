using System;

namespace SeasonsCare.Api.DTOs.Expenses
{
    public class CreateExpenseRequest
    {
        public string Title { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public string? Category { get; set; }
        public string? Notes { get; set; }
        public DateTime? ExpenseDate { get; set; }
    }
}
