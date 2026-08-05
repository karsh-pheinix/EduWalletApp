using System.ComponentModel.DataAnnotations;

namespace EduWalletApp.Models
{
    public class Account
    {
        public int Id { get; set; }

        [Required]
        public int UserId { get; set; }
        public User User { get; set; } = null!;

        [Required]
        public string Provider { get; set; } = string.Empty;

        [Required]
        public string AccountNumberMasked { get; set; } = string.Empty;

        public DateTimeOffset LinkedAt { get; set; } = DateTimeOffset.UtcNow;

        public ICollection<BankStatement> BankStatements { get; set; } = new List<BankStatement>();
    }
}