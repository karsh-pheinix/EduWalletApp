using System.ComponentModel.DataAnnotations;

namespace EduWalletApp.Models
{
    public class Budget
    {
        public int Id { get; set; }

        [Required]
        public int UserId { get; set; }
        public User User { get; set; } = null!;

        [Required]
        public DateOnly StartDate { get; set; }

        [Required]
        public DateOnly EndDate { get; set; }

        public decimal SavingsGoalPercentage { get; set; } = 0;

        public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

        public ICollection<BudgetAllocation> BudgetAllocations { get; set; } = new List<BudgetAllocation>();
        public ICollection<Report> Reports { get; set; } = new List<Report>();
    }
}