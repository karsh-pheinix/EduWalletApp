using EduWalletApp.Data;
using EduWalletApp.DTOs;
using Microsoft.EntityFrameworkCore;

namespace EduWalletApp.Services
{
    public class DashboardService : IDashboardService
    {
        private readonly ApplicationDbContext _context;
        private readonly FinanceCalculator _calculator;

        public DashboardService(ApplicationDbContext context, FinanceCalculator calculator)
        {
            _context = context;
            _calculator = calculator;
        }

        public async Task<DashboardSummaryDto> GetUserDashboardSummaryAsync(int userId)
        {
            var user = await _context.Users
                .AsNoTracking()
                .FirstOrDefaultAsync(u => u.Id == userId);

            var incomes = await _context.IncomeSources
                .AsNoTracking()
                .Where(i => i.UserId == userId && i.IsActive)
                .ToListAsync();

            var mandatoryExpenses = await _context.Expenses
                .AsNoTracking()
                .Where(e => e.UserId == userId && e.IsMandatory)
                .ToListAsync();

            var activeBudget = await _context.Budgets
                .AsNoTracking()
                .Where(b => b.UserId == userId)
                .OrderByDescending(b => b.StartDate)
                .ThenByDescending(b => b.CreatedAt)
                .FirstOrDefaultAsync();

            var savingsGoalPercentage = activeBudget?.SavingsGoalPercentage ?? 0m;

            var totalIncome = incomes.Sum(i => _calculator.ToMonthlyEquivalent(i.Amount, i.Frequency));
            var totalMandatory = mandatoryExpenses.Sum(e => _calculator.ToMonthlyEquivalent(e.Amount, e.Frequency));
            var savingsAmount = totalIncome * (savingsGoalPercentage / 100m);
            var disposable = _calculator.CalculateDisposableIncome(incomes, mandatoryExpenses, savingsGoalPercentage);

            var recentTransactions = await _context.Transactions
                .AsNoTracking()
                .Where(t => t.UserId == userId)
                .OrderByDescending(t => t.Date)
                .Take(5)
                .Select(t => new RecentTransactionDto
                {
                    Id = t.Id,
                    Merchant = t.LocationTag ?? "General Expense",
                    Amount = t.Amount,
                    CategoryName = t.Category != null ? t.Category.Name : "Uncategorized",
                    Date = t.Date
                })
                .ToListAsync();

            var categoryBreakdown = new List<CategoryBudgetOverviewDto>();
            var totalBudgeted = 0m;
            var totalSpent = 0m;

            if (activeBudget != null)
            {
                var rawCategoryBreakdown = await _context.BudgetAllocations
                    .AsNoTracking()
                    .Include(ba => ba.Transactions)
                    .Where(ba => ba.BudgetId == activeBudget.Id)
                    .Select(ba => new
                    {
                        CategoryId = ba.CategoryId,
                        CategoryName = ba.Category.Name,
                        Budgeted = ba.BudgetedAmount,
                        ActualSpent = ba.Transactions
                            .Where(t => t.UserId == userId && t.Date >= activeBudget.StartDate && t.Date <= activeBudget.EndDate)
                            .Sum(t => t.Amount)
                    })
                    .ToListAsync();

                categoryBreakdown = rawCategoryBreakdown
                    .Select(item => new CategoryBudgetOverviewDto
                    {
                        CategoryId = item.CategoryId,
                        CategoryName = item.CategoryName,
                        Budgeted = item.Budgeted,
                        ActualSpent = item.ActualSpent,
                        Remaining = item.Budgeted - item.ActualSpent,
                        Status = item.ActualSpent > item.Budgeted ? $"Overspent by R{item.ActualSpent - item.Budgeted:N2}" : "On Track"
                    })
                    .ToList();

                totalBudgeted = categoryBreakdown.Sum(item => item.Budgeted);
                totalSpent = categoryBreakdown.Sum(item => item.ActualSpent);
            }

            return new DashboardSummaryDto
            {
                TotalIncome = totalIncome,
                SavingsAmount = savingsAmount,
                TotalMandatoryExpenses = totalMandatory,
                DisposableIncome = disposable,
                DailyEquivalent = disposable / 30m,
                TotalBudgeted = totalBudgeted,
                TotalSpent = totalSpent,
                Remaining = totalBudgeted - totalSpent,
                Status = totalSpent > totalBudgeted ? $"Overspent by R{totalSpent - totalBudgeted:N2}" : "On Track",
                Archetype = user?.Archetype?.ToString(),
                RecentTransactions = recentTransactions,
                CategoryBreakdown = categoryBreakdown
            };
        }
    }
}