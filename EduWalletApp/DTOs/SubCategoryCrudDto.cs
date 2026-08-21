using System.ComponentModel.DataAnnotations;

namespace EduWalletApp.DTOs
{
    public class SubCategoryCrudDto
    {
        [Required]
        public string Name { get; set; } = string.Empty;

        [Required]
        public int CategoryId { get; set; }
    }
}