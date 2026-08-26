using System.ComponentModel.DataAnnotations;

namespace EduWalletApp.Models
{
    public class Category
    {
        public int Id { get; set; }

        [Required]
        public string Name { get; set; } = string.Empty;
        public bool IsActive { get; set; } = true;

        public ICollection<SubCategory> SubCategories { get; set; } = new List<SubCategory>();
        public ICollection<BudgetAllocation> BudgetAllocations { get; set; } = new List<BudgetAllocation>();
        public ICollection<Transaction> Transactions { get; set; } = new List<Transaction>();
        public ICollection<StatementTransaction> StatementTransactions { get; set; } = new List<StatementTransaction>();
    }
}