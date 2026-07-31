using System.ComponentModel.DataAnnotations;

namespace PersonalFinanceTracker.Api.DTOs
{
    public class AccountRequest
    {
        [Required, MaxLength(100)]
        public string Name { get; set; } = string.Empty;

        public decimal Balance { get; set; } = 0;
    }
}