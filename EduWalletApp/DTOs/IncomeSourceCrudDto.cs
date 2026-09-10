using System.ComponentModel.DataAnnotations;
using EduWalletApp.Models;

namespace EduWalletApp.DTOs
{
    public class IncomeSourceCrudDto
    {
        [Required]
        public string Source { get; set; } = string.Empty;

        [Required]
        public decimal Amount { get; set; }

        [Required]
        public IncomeFrequency Frequency { get; set; }

        public DateOnly StartDate { get; set; } = DateOnly.FromDateTime(DateTime.UtcNow);
        public DateOnly? EndDate { get; set; }
    }
}