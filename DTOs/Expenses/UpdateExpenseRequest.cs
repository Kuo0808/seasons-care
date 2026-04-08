using System;

namespace SeasonsCare.Api.DTOs.Expenses
{
    public class UpdateExpenseRequest
    {
        public string Title { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public string? Category { get; set; }
        public string? Notes { get; set; }
        public DateTime? ExpenseDate { get; set; }
        public bool IsSplitRequired { get; set; }
        
        // 用於樂觀鎖比對
        public DateTime? UpdatedAt { get; set; }
    }
}
