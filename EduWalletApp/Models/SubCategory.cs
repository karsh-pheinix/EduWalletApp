using System.ComponentModel.DataAnnotations;
using System.Transactions;

namespace EduWalletApp.Models
{
    public class SubCategory
    {
        public int Id { get; set; }

        [Required]
        public int CategoryId { get; set; }
        public Category Category { get; set; } = null!;

        [Required]
        public string Name { get; set; } = string.Empty;

        public ICollection<BudgetAllocation> BudgetAllocations { get; set; } = new List<BudgetAllocation>();
        public ICollection<Transaction> Transactions { get; set; } = new List<Transaction>();
        public ICollection<StatementTransaction> StatementTransactions { get; set; } = new List<StatementTransaction>();
    }
}