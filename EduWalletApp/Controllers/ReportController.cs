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

        private int CurrentUserId => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

        // ---------- Create ----------

        [HttpPost("{budgetId}/generate")]
        public async Task<IActionResult> GenerateReport(int budgetId)
        {
            var userId = CurrentUserId;

            var user = await _context.Users.FindAsync(userId);
            var budget = await _context.Budgets
                .FirstOrDefaultAsync(b => b.Id == budgetId && b.UserId == userId);
            if (budget == null || user == null) return NotFound();

            var today = DateOnly.FromDateTime(DateTime.UtcNow);
            var incomes = await _context.IncomeSources.Where(i => i.UserId == userId && i.IsActive
                && i.StartDate <= today && (i.EndDate == null || i.EndDate >= today)).ToListAsync();
            var mandatory = await _context.Expenses.Where(e => e.UserId == userId && e.IsMandatory
                && e.StartDate <= today && (e.EndDate == null || e.EndDate >= today)).ToListAsync();
            var actualSpend = await _context.Transactions
                .Where(t => t.UserId == userId && t.Date >= budget.StartDate && t.Date <= budget.EndDate)
                .SumAsync(t => t.Amount);

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

        // ---------- Read (all) ----------

        [HttpGet]
        public async Task<IActionResult> GetReports()
        {
            var reports = await _context.Reports
                .Where(r => r.UserId == CurrentUserId)
                .OrderByDescending(r => r.GeneratedAt)
                .Select(r => new
                {
                    r.Id,
                    r.BudgetId,
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

        // ---------- Read (single) ----------

        [HttpGet("{id}")]
        public async Task<IActionResult> GetReport(int id)
        {
            var report = await _context.Reports
                .Where(r => r.Id == id && r.UserId == CurrentUserId)
                .Select(r => new
                {
                    r.Id,
                    r.BudgetId,
                    r.ArchetypeAtGeneration,
                    r.TotalIncome,
                    r.TotalSavings,
                    r.TotalMandatoryExpenses,
                    r.TotalDisposableIncome,
                    r.TotalActualSpend,
                    r.GeneratedAt
                })
                .FirstOrDefaultAsync();

            if (report == null) return NotFound();

            return Ok(report);
        }

        // ---------- Delete ----------

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteReport(int id)
        {
            var report = await _context.Reports
                .FirstOrDefaultAsync(r => r.Id == id && r.UserId == CurrentUserId);

            if (report == null) return NotFound();

            _context.Reports.Remove(report);
            await _context.SaveChangesAsync();

            return NoContent();
        }
    }
}