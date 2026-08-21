namespace EduWalletApp.DTOs
{
    public class DashboardSummaryDto
    {
        public decimal TotalIncome { get; set; }
        public decimal SavingsAmount { get; set; }
        public decimal TotalMandatoryExpenses { get; set; }
        public decimal DisposableIncome { get; set; }
        public decimal DailyEquivalent { get; set; }
        public string? Archetype { get; set; }
        public List<RecentTransactionDto> RecentTransactions { get; set; } = new();
        public List<CategoryBudgetOverviewDto> CategoryBreakdown { get; set; } = new();
    }

    public class RecentTransactionDto
    {
        public int Id { get; set; }
        public string Merchant { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public string CategoryName { get; set; } = string.Empty;
        public DateOnly Date { get; set; }
    }

    public class CategoryBudgetOverviewDto
    {
        public int CategoryId { get; set; }
        public string CategoryName { get; set; } = string.Empty;
        public decimal Allocated { get; set; }
        public decimal Spent { get; set; }
        public decimal Remaining => Allocated - Spent;
    }
}