using System.ComponentModel.DataAnnotations;

namespace EduWalletApp.Models
{
    public class Report
    {
        public int Id { get; set; }

        [Required]
        public int UserId { get; set; }
        public User User { get; set; } = null!;

        [Required]
        public int BudgetId { get; set; }
        public Budget Budget { get; set; } = null!;

        public Archetype ArchetypeAtGeneration { get; set; }
        public decimal TotalIncome { get; set; }
        public decimal TotalSavings { get; set; }
        public decimal TotalMandatoryExpenses { get; set; }
        public decimal TotalDisposableIncome { get; set; }
        public decimal TotalActualSpend { get; set; }

        public DateTimeOffset GeneratedAt { get; set; } = DateTimeOffset.UtcNow;
    }
}