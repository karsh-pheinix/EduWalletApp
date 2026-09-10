namespace EduWalletApp.DTOs
{
    public class IncomeSourceDto
    {
        public string Source { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public Models.IncomeFrequency Frequency { get; set; }
        public DateOnly StartDate { get; set; } = DateOnly.FromDateTime(DateTime.UtcNow);
    }
}