namespace PersonalFinanceTracker.Api.DTOs
{
    public class BudgetResponse
    {
        public int Id { get; set; }
        public int CategoryId { get; set; }
        public string CategoryName { get; set; } = string.Empty;
        public decimal MonthlyLimit { get; set; }
        public int Month { get; set; }
        public int Year { get; set; }
        public decimal AmountSpent { get; set; }
        public decimal RemainingAmount { get; set; }
    }
}