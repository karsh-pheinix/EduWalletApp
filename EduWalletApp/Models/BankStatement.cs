using System.ComponentModel.DataAnnotations;

namespace EduWalletApp.Models
{
    public enum StatementFileType
    {
        Csv = 0,
        Pdf = 1
    }

    public class BankStatement
    {
        public int Id { get; set; }

        [Required]
        public int UserId { get; set; }
        public User User { get; set; } = null!;

        public int? AccountId { get; set; }
        public Account? Account { get; set; }

        [Required]
        public string FileName { get; set; } = string.Empty;

        public StatementFileType FileType { get; set; }

        public DateTimeOffset UploadedAt { get; set; } = DateTimeOffset.UtcNow;

        public ICollection<StatementTransaction> StatementTransactions { get; set; } = new List<StatementTransaction>();
    }
}