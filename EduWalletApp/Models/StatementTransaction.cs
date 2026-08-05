using System.ComponentModel.DataAnnotations;

namespace EduWalletApp.Models
{
    public class StatementTransaction
    {
        public int Id { get; set; }

        [Required]
        public int BankStatementId { get; set; }
        public BankStatement BankStatement { get; set; } = null!;

        // Nullable — a freshly parsed row might not be categorized yet
        public int? CategoryId { get; set; }
        public Category? Category { get; set; }

        public int? SubCategoryId { get; set; }
        public SubCategory? SubCategory { get; set; }

        [Required]
        public decimal Amount { get; set; }

        [Required]
        public DateOnly Date { get; set; }

        [Required]
        public string Description { get; set; } = string.Empty; // raw line text from the parsed statement

        public string? LocationTag { get; set; }
    }
}