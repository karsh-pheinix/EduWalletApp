namespace EduWalletApp.DTOs
{
    public class DashboardSummaryDto
    {
        public decimal TotalIncome { get; set; }
        public decimal SavingsAmount { get; set; }
        public decimal TotalMandatoryExpenses { get; set; }
        public decimal DisposableIncome { get; set; }
        public decimal DailyEquivalent { get; set; }
        public decimal TotalBudgeted { get; set; }
        public decimal TotalSpent { get; set; }
        public decimal Remaining { get; set; }
        public string Status { get; set; } = "On Track";
        public string? Archetype { get; set; }
        public decimal ForecastDailySpend { get; set; }
        public decimal Forecast30DayBalance { get; set; }
        public bool ForecastAtRisk { get; set; }
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
        public decimal Budgeted { get; set; }
        public decimal ActualSpent { get; set; }
        public decimal Remaining { get; set; }
        public string Status { get; set; } = "On Track";
    }
}