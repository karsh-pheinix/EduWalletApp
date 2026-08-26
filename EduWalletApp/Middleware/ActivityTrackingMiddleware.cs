using System.Collections.Concurrent;
using System.Security.Claims;
using EduWalletApp.Data;
using Microsoft.EntityFrameworkCore;

namespace EduWalletApp.Middleware;

public class ActivityTrackingMiddleware(RequestDelegate next)
{
    private static readonly ConcurrentDictionary<int, DateTimeOffset> LastUpdates = new();

    public async Task InvokeAsync(HttpContext context, ApplicationDbContext db)
    {
        if (context.User.Identity?.IsAuthenticated == true &&
            int.TryParse(context.User.FindFirstValue(ClaimTypes.NameIdentifier), out var userId))
        {
            var now = DateTimeOffset.UtcNow;
            if (!LastUpdates.TryGetValue(userId, out var lastUpdate) || now - lastUpdate >= TimeSpan.FromMinutes(5))
            {
                LastUpdates[userId] = now;
                await db.Users.Where(user => user.Id == userId)
                    .ExecuteUpdateAsync(setters => setters.SetProperty(user => user.LastActiveAt, now));
            }
        }

        await next(context);
    }
}