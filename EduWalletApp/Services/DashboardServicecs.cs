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
                .OrderByDescending(b => b.CreatedAt)
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

            if (activeBudget != null)
            {
                categoryBreakdown = await _context.BudgetAllocations
                    .AsNoTracking()
                    .Where(ba => ba.BudgetId == activeBudget.Id)
                    .Select(ba => new CategoryBudgetOverviewDto
                    {
                        CategoryId = ba.CategoryId,
                        CategoryName = ba.Category.Name,
                        Allocated = ba.Amount,
                        Spent = _context.Transactions
                            .Where(t => t.UserId == userId && t.CategoryId == ba.CategoryId)
                            .Sum(t => (decimal?)t.Amount) ?? 0m
                    })
                    .ToListAsync();
            }

            return new DashboardSummaryDto
            {
                TotalIncome = totalIncome,
                SavingsAmount = savingsAmount,
                TotalMandatoryExpenses = totalMandatory,
                DisposableIncome = disposable,
                DailyEquivalent = disposable / 22m,
                Archetype = user?.Archetype?.ToString(),
                RecentTransactions = recentTransactions,
                CategoryBreakdown = categoryBreakdown
            };
        }
    }
}