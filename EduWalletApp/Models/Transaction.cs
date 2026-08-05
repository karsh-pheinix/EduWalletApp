using System.ComponentModel.DataAnnotations;

namespace EduWalletApp.Models
{
    public class Transaction
    {
        public int Id { get; set; }

        [Required]
        public int UserId { get; set; }
        public User User { get; set; } = null!;

        public int? BudgetAllocationId { get; set; }
        public BudgetAllocation? BudgetAllocation { get; set; }

        [Required]
        public int CategoryId { get; set; }
        public Category Category { get; set; } = null!;

        public int? SubCategoryId { get; set; }
        public SubCategory? SubCategory { get; set; }

        [Required]
        public decimal Amount { get; set; }

        [Required]
        public DateOnly Date { get; set; }

        public string? LocationTag { get; set; }

        public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    }
}