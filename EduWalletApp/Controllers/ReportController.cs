using EduWalletApp.Data;
using EduWalletApp.Models;
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
    public class ReportController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly FinanceCalculator _calculator;

        public ReportController(ApplicationDbContext context, FinanceCalculator calculator)
        {
            _context = context;
            _calculator = calculator;
        }

        [HttpPost("{budgetId}/generate")]
        public async Task<IActionResult> GenerateReport(int budgetId)
        {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

            var user = await _context.Users.FindAsync(userId);
            var budget = await _context.Budgets
                .FirstOrDefaultAsync(b => b.Id == budgetId && b.UserId == userId);
            if (budget == null || user == null) return NotFound();

            var incomes = await _context.IncomeSources.Where(i => i.UserId == userId && i.IsActive).ToListAsync();
            var mandatory = await _context.Expenses.Where(e => e.UserId == userId && e.IsMandatory).ToListAsync();
            var actualSpend = await _context.Transactions
                .Where(t => t.UserId == userId).SumAsync(t => t.Amount);

            var totalIncome = incomes.Sum(i => _calculator.ToMonthlyEquivalent(i.Amount, i.Frequency));
            var totalMandatory = mandatory.Sum(e => _calculator.ToMonthlyEquivalent(e.Amount, e.Frequency));
            var savingsAmount = totalIncome * (budget.SavingsGoalPercentage / 100);
            var disposable = _calculator.CalculateDisposableIncome(incomes, mandatory, budget.SavingsGoalPercentage);

            var report = new Report
            {
                UserId = userId,
                BudgetId = budgetId,
                ArchetypeAtGeneration = user.Archetype ?? Archetype.Avoider,
                TotalIncome = totalIncome,
                TotalSavings = savingsAmount,
                TotalMandatoryExpenses = totalMandatory,
                TotalDisposableIncome = disposable,
                TotalActualSpend = actualSpend
            };

            _context.Reports.Add(report);
            await _context.SaveChangesAsync();

            return Ok(new { reportId = report.Id });
        }

        [HttpGet]
        public async Task<IActionResult> GetReports()
        {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

            var reports = await _context.Reports
                .Where(r => r.UserId == userId)
                .OrderByDescending(r => r.GeneratedAt)
                .Select(r => new
                {
                    r.Id,
                    r.ArchetypeAtGeneration,
                    r.TotalIncome,
                    r.TotalSavings,
                    r.TotalMandatoryExpenses,
                    r.TotalDisposableIncome,
                    r.TotalActualSpend,
                    r.GeneratedAt
                })
                .ToListAsync();

            return Ok(reports);
        }
    }
}