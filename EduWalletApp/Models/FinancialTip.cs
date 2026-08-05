using System.ComponentModel.DataAnnotations;

namespace EduWalletApp.Models
{
    public enum TipCategory
    {
        Saving = 0,
        Budgeting = 1,
        StudentLife = 2,
        General = 3
    }

    public class FinancialTip
    {
        public int Id { get; set; }

        [Required]
        public string Title { get; set; } = string.Empty;

        [Required]
        public string Content { get; set; } = string.Empty;

        public TipCategory Category { get; set; }

        public DateTimeOffset PublishedAt { get; set; } = DateTimeOffset.UtcNow;
    }
}