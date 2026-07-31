using System.ComponentModel.DataAnnotations;

namespace PersonalFinanceTracker.Api.DTOs
{
    public class TransactionRequest
    {
        [Required]
        public int AccountId { get; set; }

        [Required]
        public int CategoryId { get; set; }

        [Required]
        public decimal Amount { get; set; }

        [Required, MaxLength(10)]
        public string Type { get; set; } = "Expense"; // "Income" or "Expense"

        [MaxLength(255)]
        public string? Description { get; set; }

        public DateTime Date { get; set; } = DateTime.UtcNow;
    }
}