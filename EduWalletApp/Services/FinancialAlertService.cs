using EduWalletApp.Data;
using EduWalletApp.Models;
using Microsoft.EntityFrameworkCore;

namespace EduWalletApp.Services;

public class FinancialAlertService(ApplicationDbContext context, FinanceCalculator calculator) : IFinancialAlertService
{
    public async Task CheckAndCreateAlertsAsync(int userId)
    {
        var budget = await context.Budgets.Include(b => b.BudgetAllocations).ThenInclude(a => a.Transactions)
            .Where(b => b.UserId == userId).OrderByDescending(b => b.CreatedAt).FirstOrDefaultAsync();
        if (budget is null) return;
        var notifications = new List<Notification>();
        foreach (var allocation in budget.BudgetAllocations)
        {
            var spent = allocation.Transactions.Where(t => t.Date >= budget.StartDate && t.Date <= budget.EndDate).Sum(t => t.Amount);
            if (spent > allocation.BudgetedAmount)
                await AddIfMissing(userId, NotificationType.LeakageAlert, $"You've gone over budget in category {allocation.CategoryId} this period.", $"over-budget:{budget.Id}:{allocation.Id}");
        }
        var incomeSources = await context.IncomeSources.Where(i => i.UserId == userId && i.IsActive).ToListAsync();
        var income = incomeSources.Sum(i => calculator.ToMonthlyEquivalent(i.Amount, i.Frequency));
        var spend = await context.Transactions.Where(t => t.UserId == userId && t.Date >= budget.StartDate && t.Date <= budget.EndDate).SumAsync(t => t.Amount);
        if (spend > income) await AddIfMissing(userId, NotificationType.ReviewFlag, "Your spending has exceeded your income this month.", $"negative-income:{budget.Id}");
        var expenses = await context.Expenses.AnyAsync(e => e.UserId == userId);
        if (expenses && income <= 0) await AddIfMissing(userId, NotificationType.General, "We don't see any income logged this period — add one so your budget stays accurate.", $"no-income:{budget.Id}");
        await context.SaveChangesAsync();
    }

    private async Task AddIfMissing(int userId, NotificationType type, string message, string key)
    {
        var exists = await context.Notifications.AnyAsync(n => n.UserId == userId && n.AlertKey == key && !n.IsRead);
        if (!exists) context.Notifications.Add(new Notification { UserId = userId, Type = type, Message = message, AlertKey = key });
    }
}