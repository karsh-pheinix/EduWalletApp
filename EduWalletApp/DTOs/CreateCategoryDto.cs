using System.ComponentModel.DataAnnotations;

namespace EduWalletApp.Models.Dtos
{
    public class CreateCategoryDto
    {
        [Required]
        public string Name { get; set; } = string.Empty;
    }
}