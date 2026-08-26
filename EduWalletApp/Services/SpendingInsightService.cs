using EduWalletApp.Data;
using EduWalletApp.DTOs;
using Microsoft.EntityFrameworkCore;

namespace EduWalletApp.Services
{
    public class SpendingInsightService : ISpendingInsightService
    {
        private readonly ApplicationDbContext _context;

        public SpendingInsightService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<SpendingInsightDto> GetInsightsForStatementAsync(int statementId, int userId)
        {
            var rows = await _context.StatementTransactions
                .Where(st => st.BankStatementId == statementId && st.BankStatement.UserId == userId && st.Amount < 0)
                .Select(st => new
                {
                    Amount = -st.Amount,
                    CategoryName = st.Category != null ? st.Category.Name : "Uncategorized"
                })
                .ToListAsync();

            var totalSpend = rows.Sum(r => r.Amount);
            var breakdown = rows
                .GroupBy(r => r.CategoryName)
                .Select(group => new CategoryBreakdownDto
                {
                    CategoryName = group.Key,
                    Amount = group.Sum(r => r.Amount),
                    Percentage = totalSpend > 0 ? Math.Round(group.Sum(r => r.Amount) / totalSpend * 100, 1) : 0
                })
                .OrderByDescending(b => b.Amount)
                .ToList();

            var topCategory = breakdown.FirstOrDefault();

            return new SpendingInsightDto
            {
                TotalSpend = totalSpend,
                Breakdown = breakdown,
                TopCategoryName = topCategory?.CategoryName,
                TopCategoryPercentage = topCategory?.Percentage ?? 0,
                HabitTips = BuildHabitTips(breakdown)
            };
        }

        private static List<string> BuildHabitTips(List<CategoryBreakdownDto> breakdown)
        {
            var tips = new List<string>();

            foreach (var entry in breakdown.Take(2))
            {
                if (entry.Percentage < 15) continue;

                var name = entry.CategoryName.ToLowerInvariant();
                var tip = name switch
                {
                    var value when value.Contains("food") || value.Contains("takeout") || value.Contains("restaurant") =>
                        $"{entry.CategoryName} made up {entry.Percentage}% of this statement — try meal prepping a few days a week to cut this down.",
                    var value when value.Contains("transport") || value.Contains("uber") || value.Contains("taxi") =>
                        $"{entry.CategoryName} made up {entry.Percentage}% of this statement — carpooling or a monthly transport pass could work out cheaper than per-trip fares.",
                    var value when value.Contains("entertainment") || value.Contains("subscription") =>
                        $"{entry.CategoryName} made up {entry.Percentage}% of this statement — check for subscriptions you're not using and consider sharing plans with roommates.",
                    var value when value.Contains("airtime") || value.Contains("data") =>
                        $"{entry.CategoryName} made up {entry.Percentage}% of this statement — a monthly data bundle is usually cheaper than buying data as you go.",
                    var value when value.Contains("grocer") =>
                        $"{entry.CategoryName} made up {entry.Percentage}% of this statement — shopping with a list and comparing prices between stores can meaningfully lower this.",
                    _ =>
                        $"{entry.CategoryName} made up {entry.Percentage}% of this statement — that's your biggest spend category, worth a closer look before your next budget period."
                };
                tips.Add(tip);
            }

            if (tips.Count == 0)
                tips.Add("Your spending is fairly spread out across categories — no single area stands out as a habit to fix right now.");

            return tips;
        }
    }
}
