namespace EduWalletApp.DTOs
{
    public class UpdateBudgetAllocationDto
    {
        public int CategoryId { get; set; }
        public int? SubCategoryId { get; set; }
        public decimal Amount { get; set; }
        public string? LocationTag { get; set; }
    }
}