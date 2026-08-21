using System.ComponentModel.DataAnnotations;
using EduWalletApp.Models;

namespace EduWalletApp.DTOs
{
    public class ExpenseCrudDto
    {
        [Required]
        public string Label { get; set; } = string.Empty;

        [Required]
        public ExpenseType Type { get; set; }

        [Required]
        public decimal Amount { get; set; }

        [Required]
        public ExpenseFrequency Frequency { get; set; }

        public bool IsMandatory { get; set; } = true;
    }
}