namespace EduWalletApp.DTOs
{
    public class UpdateBudgetDto
    {
        public DateOnly StartDate { get; set; }
        public DateOnly EndDate { get; set; }
        public decimal SavingsGoalPercentage { get; set; }
    }
}