using System;

namespace SeasonsCare.Api.DTOs.Expenses
{
    public class ExpenseResponse
    {
        public Guid Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public string? Category { get; set; }
        public string? Notes { get; set; }
        public DateTime ExpenseDate { get; set; }
        public bool IsSplitRequired { get; set; }
        
        public Guid CareGroupId { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public string CreatedBy { get; set; } = string.Empty;
    }
}
