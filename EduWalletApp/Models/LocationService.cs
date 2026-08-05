using System.ComponentModel.DataAnnotations;

namespace EduWalletApp.Models
{
    public class LocationService
    {
        public int Id { get; set; }

        [Required]
        public string University { get; set; } = string.Empty;

        [Required]
        public string Name { get; set; } = string.Empty;

        [Required]
        public string Category { get; set; } = string.Empty;
    }
}