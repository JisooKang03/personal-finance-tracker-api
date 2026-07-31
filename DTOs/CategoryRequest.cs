using System.ComponentModel.DataAnnotations;

namespace PersonalFinanceTracker.Api.DTOs
{
    public class CategoryRequest
    {
        [Required, MaxLength(50)]
        public string Name { get; set; } = string.Empty;

        [Required, MaxLength(10)]
        public string Type { get; set; } = "Expense"; // "Income" or "Expense"
    }
}