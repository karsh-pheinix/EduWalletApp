namespace EduWalletApp.DTOs
{
    public class CategoryBreakdownDto
    {
        public string CategoryName { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public decimal Percentage { get; set; }
    }

    public class SpendingInsightDto
    {
        public decimal TotalSpend { get; set; }
        public List<CategoryBreakdownDto> Breakdown { get; set; } = new();
        public string? TopCategoryName { get; set; }
        public decimal TopCategoryPercentage { get; set; }
        public List<string> HabitTips { get; set; } = new();
    }
}
