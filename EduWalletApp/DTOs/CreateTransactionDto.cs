namespace EduWalletApp.DTOs
{
    public class CreateTransactionDto
    {
        public int? BudgetAllocationId { get; set; }
        public int CategoryId { get; set; }
        public int? SubCategoryId { get; set; }
        public decimal Amount { get; set; }
        public DateOnly Date { get; set; }
        public string? LocationTag { get; set; }
    }
}