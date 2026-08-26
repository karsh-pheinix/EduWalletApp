using System.ComponentModel.DataAnnotations;
using System.Transactions;

namespace EduWalletApp.Models
{
    public class BudgetAllocation
    {
        public int Id { get; set; }

        [Required]
        public int BudgetId { get; set; }
        public Budget Budget { get; set; } = null!;

        [Required]
        public int CategoryId { get; set; }
        public Category Category { get; set; } = null!;

        public int? SubCategoryId { get; set; }
        public SubCategory? SubCategory { get; set; }

        [Required]
        public decimal BudgetedAmount { get; set; }

        public string? LocationTag { get; set; }

        public bool IsUnderReview { get; set; } = false;

        public decimal? SuggestedCap { get; set; }

        public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

        public ICollection<Transaction> Transactions { get; set; } = new List<Transaction>();
    }
}