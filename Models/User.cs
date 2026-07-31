using System.ComponentModel.DataAnnotations;

namespace PersonalFinanceTracker.Api.Models
{
    public class User
    {
        public int Id { get; set; }

        [Required, MaxLength(100)]
        public string FullName { get; set; } = string.Empty;

        [Required, MaxLength(256)]
        public string Email { get; set; } = string.Empty;

        [Required]
        public string PasswordHash { get; set; } = string.Empty;

        [Required, MaxLength(20)]
        public string Role { get; set; } = "User"; // "User" or "Admin"

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation
        public ICollection<Account> Accounts { get; set; } = new List<Account>();
        public ICollection<Budget> Budgets { get; set; } = new List<Budget>();
    }
}