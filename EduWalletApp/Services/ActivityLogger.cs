using EduWalletApp.Data;
using EduWalletApp.Models;

namespace EduWalletApp.Services;

public class ActivityLogger(ApplicationDbContext context) : IActivityLogger
{
    public async Task LogAsync(int userId, string action, string? details = null)
    {
        context.ActivityLogs.Add(new ActivityLog
        {
            UserId = userId,
            Action = action,
            Details = details
        });
        await context.SaveChangesAsync();
    }
}