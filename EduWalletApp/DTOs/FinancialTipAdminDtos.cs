using System.ComponentModel.DataAnnotations;
using EduWalletApp.Models;

namespace EduWalletApp.DTOs;

public class CreateTipDto
{
    [Required] public string Title { get; set; } = string.Empty;
    [Required] public string Content { get; set; } = string.Empty;
    public TipCategory Category { get; set; }
    public TipTriggerType TriggerType { get; set; }
    public decimal? ThresholdPercentage { get; set; }
    public int? TargetCategoryId { get; set; }
}

public class UpdateTipDto : CreateTipDto { }

public record TipAdminDto(int Id, string Title, string Content, TipCategory Category, DateTimeOffset PublishedAt, TipTriggerType TriggerType, decimal? ThresholdPercentage, int? TargetCategoryId);

public record StudentTipDto(int Id, string Title, string Content, TipCategory Category, DateTimeOffset PublishedAt);