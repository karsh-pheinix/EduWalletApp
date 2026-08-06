using EduWalletApp.Data;
using EduWalletApp.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace EduWalletApp.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class DashboardController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly FinanceCalculator _calculator;

        public DashboardController(ApplicationDbContext context, FinanceCalculator calculator)
        {
            _context = context;
            _calculator = calculator;
        }

        [HttpGet("summary")]
        public async Task<IActionResult> GetSummary()
        {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

            var user = await _context.Users.FindAsync(userId);
            var incomes = await _context.IncomeSources.Where(i => i.UserId == userId && i.IsActive).ToListAsync();
            var mandatoryExpenses = await _context.Expenses
                .Where(e => e.UserId == userId && e.IsMandatory).ToListAsync();
            var budget = await _context.Budgets
                .Where(b => b.UserId == userId)
                .OrderByDescending(b => b.CreatedAt)
                .FirstOrDefaultAsync();

            var savingsGoal = budget?.SavingsGoalPercentage ?? 0;

            var totalIncome = incomes.Sum(i => _calculator.ToMonthlyEquivalent(i.Amount, i.Frequency));
            var totalMandatory = mandatoryExpenses.Sum(e => _calculator.ToMonthlyEquivalent(e.Amount, e.Frequency));
            var savingsAmount = totalIncome * (savingsGoal / 100);
            var disposable = _calculator.CalculateDisposableIncome(incomes, mandatoryExpenses, savingsGoal);

            return Ok(new
            {
                totalIncome,
                savingsAmount,
                totalMandatory,
                disposableIncome = disposable,
                dailyEquivalent = disposable / 22,
                archetype = user?.Archetype?.ToString()
            });
        }
    }
}