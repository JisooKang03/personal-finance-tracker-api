using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PersonalFinanceTracker.Api.Models
{
    public class Transaction
    {
        public int Id { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal Amount { get; set; }

        [Required, MaxLength(10)]
        public string Type { get; set; } = "Expense"; // "Income" or "Expense"

        [MaxLength(255)]
        public string? Description { get; set; }

        public DateTime Date { get; set; } = DateTime.UtcNow;

        public int AccountId { get; set; }
        public Account? Account { get; set; }

        public int CategoryId { get; set; }
        public Category? Category { get; set; }

        // Soft delete pattern (banking-style: never hard-delete financial records)
        public bool IsDeleted { get; set; } = false;

        public string? ReceiptUrl { get; set; } // Blob Storage URL

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}