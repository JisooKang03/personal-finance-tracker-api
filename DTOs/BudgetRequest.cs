using System.ComponentModel.DataAnnotations;

namespace PersonalFinanceTracker.Api.DTOs
{
    public class BudgetRequest
    {
        [Required]
        public int CategoryId { get; set; }

        [Required]
        public decimal MonthlyLimit { get; set; }

        [Required, Range(1, 12)]
        public int Month { get; set; }

        [Required, Range(2000, 2100)]
        public int Year { get; set; }
    }
}