namespace PersonalFinanceTracker.Api.DTOs
{
    public class AccountResponse
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public decimal Balance { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}