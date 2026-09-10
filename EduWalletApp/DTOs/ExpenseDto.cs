namespace EduWalletApp.DTOs
{
    public class ExpenseDto
    {
        public Models.ExpenseType Type { get; set; }
        public string Label { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public Models.ExpenseFrequency Frequency { get; set; }
        public DateOnly StartDate { get; set; } = DateOnly.FromDateTime(DateTime.UtcNow);
    }
}