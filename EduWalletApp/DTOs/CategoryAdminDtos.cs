using System.ComponentModel.DataAnnotations;

namespace EduWalletApp.DTOs;

public class CreateAdminCategoryDto
{
    [Required, MaxLength(100)] public string Name { get; set; } = string.Empty;
}

public class UpdateAdminCategoryDto
{
    [Required, MaxLength(100)] public string Name { get; set; } = string.Empty;
    public bool IsActive { get; set; }
}

public record CategoryAdminDto(int Id, string Name, bool IsActive, IEnumerable<object> SubCategories);