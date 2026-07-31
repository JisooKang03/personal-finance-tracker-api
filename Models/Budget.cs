using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PersonalFinanceTracker.Api.Models
{
    public class Budget
    {
        public int Id { get; set; }

        public int UserId { get; set; }
        public User? User { get; set; }

        public int CategoryId { get; set; }
        public Category? Category { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal MonthlyLimit { get; set; }

        public int Month { get; set; } // 1-12
        public int Year { get; set; }
    }
}