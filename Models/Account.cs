using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PersonalFinanceTracker.Api.Models
{
    public class Account
    {
        public int Id { get; set; }

        [Required, MaxLength(100)]
        public string Name { get; set; } = string.Empty; // e.g. "Checking", "Savings"

        [Column(TypeName = "decimal(18,2)")]
        public decimal Balance { get; set; }

        public int UserId { get; set; }
        public User? User { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public ICollection<Transaction> Transactions { get; set; } = new List<Transaction>();
    }
}