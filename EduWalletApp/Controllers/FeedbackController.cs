using System.Security.Claims;
using EduWalletApp.Data;
using EduWalletApp.DTOs;
using EduWalletApp.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EduWalletApp.Controllers;

[ApiController]
[Route("api/feedback")]
[Authorize]
public class FeedbackController(ApplicationDbContext context) : ControllerBase
{
    private int CurrentUserId => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    [HttpGet("should-prompt")]
    public async Task<ActionResult<bool>> ShouldPrompt()
    {
        var hasRecentFeedback = await context.Feedback.AnyAsync(entry => entry.UserId == CurrentUserId && entry.CreatedAt > DateTimeOffset.UtcNow.AddDays(-30));
        return Ok(!hasRecentFeedback);
    }

    [HttpPost]
    public async Task<IActionResult> Create(CreateFeedbackDto dto)
    {
        if (!ModelState.IsValid) return ValidationProblem(ModelState);
        context.Feedback.Add(new Feedback { UserId = CurrentUserId, Rating = dto.Rating, Comment = dto.Comment?.Trim() });
        await context.SaveChangesAsync();
        return Ok(new { message = "Thank you for your feedback." });
    }
}