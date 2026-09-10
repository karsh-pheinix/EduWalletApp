using System.ComponentModel.DataAnnotations;

namespace EduWalletApp.Models
{
    public enum IncomeFrequency
    {
        Monthly = 0,
        Weekly = 1,
        Daily = 2,
        OnceOff = 3
    }

    public class IncomeSource
    {
        public int Id { get; set; }

        [Required]
        public int UserId { get; set; }
        public User User { get; set; } = null!;

        [Required]
        public string Source { get; set; } = string.Empty;

        [Required]
        public decimal Amount { get; set; }

        public IncomeFrequency Frequency { get; set; }

        public DateOnly StartDate { get; set; } = DateOnly.FromDateTime(DateTime.UtcNow);
        public DateOnly? EndDate { get; set; }

        public bool IsActive { get; set; } = true;

        public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    }
}