using System.ComponentModel.DataAnnotations;

namespace EduWalletApp.Models
{
    public enum ExpenseType
    {
        Rent = 0,
        Transport = 1,
        Tuition = 2,
        Other = 3
    }

    public enum ExpenseFrequency
    {
        Monthly = 0,
        Daily = 1,
        OnceOff = 2
    }

    public class Expense
    {
        public int Id { get; set; }

        [Required]
        public int UserId { get; set; }
        public User User { get; set; } = null!;

        public ExpenseType Type { get; set; }

        [Required]
        public string Label { get; set; } = string.Empty;

        [Required]
        public decimal Amount { get; set; }

        public ExpenseFrequency Frequency { get; set; }

        public DateOnly StartDate { get; set; } = DateOnly.FromDateTime(DateTime.UtcNow);
        public DateOnly? EndDate { get; set; }

        public bool IsMandatory { get; set; } = true;

        public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    }
}