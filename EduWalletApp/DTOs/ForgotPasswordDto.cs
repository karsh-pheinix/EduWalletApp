using System.ComponentModel.DataAnnotations;

namespace EduWalletApp.DTOs
{
    public class ForgotPasswordDto
    {
        [Required]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;
    }
}
