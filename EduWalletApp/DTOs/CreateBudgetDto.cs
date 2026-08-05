namespace EduWalletApp.DTOs
{
    public class CreateBudgetDto
    {
        public DateOnly StartDate { get; set; }
        public DateOnly EndDate { get; set; }
    }

    public class BudgetAllocationDto
    {
        public int CategoryId { get; set; }
        public int? SubCategoryId { get; set; }
        public decimal Amount { get; set; }
        public string? LocationTag { get; set; }
    }
}