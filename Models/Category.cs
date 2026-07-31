using System.ComponentModel.DataAnnotations;

namespace PersonalFinanceTracker.Api.Models
{
    public class Category
    {
        public int Id { get; set; }

        [Required, MaxLength(50)]
        public string Name { get; set; } = string.Empty; // e.g. "Groceries", "Rent"

        [Required, MaxLength(10)]
        public string Type { get; set; } = "Expense"; // "Income" or "Expense"

        public ICollection<Transaction> Transactions { get; set; } = new List<Transaction>();
        public ICollection<Budget> Budgets { get; set; } = new List<Budget>();
    }
}