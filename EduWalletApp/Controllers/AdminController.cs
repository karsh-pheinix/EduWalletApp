using System.Security.Claims;
using System.Text;
using EduWalletApp.Data;
using EduWalletApp.DTOs;
using EduWalletApp.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EduWalletApp.Controllers;

[ApiController]
[Route("api/admin")]
[Authorize(Roles = "Admin")]
public class AdminController(ApplicationDbContext context, UserManager<User> userManager) : ControllerBase
{
    [HttpGet("summary")]
    public async Task<ActionResult<AdminSummaryDto>> Summary()
    {
        return Ok(new AdminSummaryDto(
            await context.Users.CountAsync(),
            await context.Users.CountAsync(user => !user.LockoutEnabled || !user.LockoutEnd.HasValue || user.LockoutEnd <= DateTimeOffset.UtcNow),
            await context.Users.CountAsync(user => user.LockoutEnabled && user.LockoutEnd.HasValue && user.LockoutEnd > DateTimeOffset.UtcNow),
            await context.Feedback.CountAsync(),
            await context.ActivityLogs.CountAsync()));
    }

    [HttpGet("users")]
    public async Task<ActionResult<IEnumerable<AdminUserDto>>> Users([FromQuery] string? search = null)
    {
        var query = context.Users.AsNoTracking();
        if (!string.IsNullOrWhiteSpace(search))
            query = query.Where(user => (user.Email ?? "").Contains(search) || user.FirstName.Contains(search) || user.LastName.Contains(search) || user.StudentIdNumber.Contains(search));

        var users = await query.OrderBy(user => user.LastName).Select(user => new AdminUserDto(
            user.Id, user.Email, user.FirstName, user.LastName, user.StudentIdNumber,
            user.LockoutEnabled && user.LockoutEnd > DateTimeOffset.UtcNow,
            user.LastActiveAt, user.CreatedAt)).ToListAsync();
        return Ok(users);
    }

    [HttpPatch("users/{id:int}/status")]
    public async Task<IActionResult> UpdateStatus(int id, UpdateUserStatusDto dto)
    {
        var user = await userManager.FindByIdAsync(id.ToString());
        if (user is null) return NotFound();
        if (user.Id == int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!)) return BadRequest("You cannot disable your own account.");

        user.LockoutEnabled = dto.Disabled;
        user.LockoutEnd = dto.Disabled ? DateTimeOffset.MaxValue : null;
        await userManager.UpdateAsync(user);
        return NoContent();
    }

    [HttpGet("users/{id:int}/activity")]
    public async Task<ActionResult<IEnumerable<AdminActivityDto>>> UserActivity(int id)
    {
        var activity = await context.ActivityLogs.AsNoTracking().Where(log => log.UserId == id)
            .OrderByDescending(log => log.CreatedAt)
            .Select(log => new AdminActivityDto(log.Id, log.UserId, log.User.Email, log.Action, log.Details, log.CreatedAt)).ToListAsync();
        return Ok(activity);
    }

    [HttpGet("activity")]
    public async Task<ActionResult<IEnumerable<AdminActivityDto>>> Activity([FromQuery] int take = 100)
    {
        var activity = await context.ActivityLogs.AsNoTracking().OrderByDescending(log => log.CreatedAt)
            .Take(Math.Clamp(take, 1, 1000))
            .Select(log => new AdminActivityDto(log.Id, log.UserId, log.User.Email, log.Action, log.Details, log.CreatedAt)).ToListAsync();
        return Ok(activity);
    }

    [HttpGet("feedback")]
    public async Task<ActionResult<IEnumerable<AdminFeedbackDto>>> Feedback()
    {
        var feedback = await context.Feedback.AsNoTracking().OrderByDescending(entry => entry.CreatedAt)
            .Select(entry => new AdminFeedbackDto(entry.Id, entry.UserId, entry.User.Email, entry.Rating, entry.Comment, entry.CreatedAt)).ToListAsync();
        return Ok(feedback);
    }

    [HttpGet("activity/export")]
    public async Task<IActionResult> ExportActivity([FromQuery] DateTimeOffset? from = null, [FromQuery] DateTimeOffset? to = null)
    {
        var query = context.ActivityLogs.AsNoTracking().AsQueryable();

        if (from.HasValue) query = query.Where(log => log.CreatedAt >= from.Value);
        if (to.HasValue) query = query.Where(log => log.CreatedAt <= to.Value);

        var logs = await query
            .OrderBy(log => log.CreatedAt)
            .Select(log => new { log.Id, log.UserId, Email = log.User.Email, log.Action, log.Details, log.CreatedAt })
            .ToListAsync();

        var csv = new StringBuilder();
        csv.AppendLine("Id,UserId,Email,Action,Details,CreatedAtUtc");
        foreach (var log in logs)
        {
            csv.AppendLine(string.Join(",",
                log.Id,
                log.UserId,
                CsvEscape(log.Email ?? ""),
                CsvEscape(log.Action),
                CsvEscape(log.Details ?? ""),
                log.CreatedAt.UtcDateTime.ToString("yyyy-MM-dd HH:mm:ss")));
        }

        var fileName = $"eduwallet-activity-log-{DateTime.UtcNow:yyyyMMdd-HHmm}.csv";
        return File(Encoding.UTF8.GetBytes(csv.ToString()), "text/csv", fileName);
    }

    private static string CsvEscape(string value)
    {
        if (value.Contains(',') || value.Contains('"') || value.Contains('\n'))
            return "\"" + value.Replace("\"", "\"\"") + "\"";
        return value;
    }
}