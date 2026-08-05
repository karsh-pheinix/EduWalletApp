namespace EduWalletApp.DTOs
{
    public class BankStatementUploadDto
    {
        public IFormFile File { get; set; } = null!;
        public EduWalletApp.Models.StatementFileType FileType { get; set; }
        public int? AccountId { get; set; }
    }
}