using System.ComponentModel.DataAnnotations;

namespace EduWalletApp.DTOs;

public class CreateFeedbackDto
{
    [Range(1, 5)]
    public int Rating { get; set; }

    [MaxLength(1000)]
    public string? Comment { get; set; }
}