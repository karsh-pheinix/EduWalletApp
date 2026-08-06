using EduWalletApp.Data;
using EduWalletApp.DTOs;
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
    public class BudgetController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly FinanceCalculator _calculator;

        public BudgetController(ApplicationDbContext context, FinanceCalculator calculator)
        {
            _context = context;
            _calculator = calculator;
        }

        [HttpPost]
        public async Task<IActionResult> CreateBudget([FromBody] CreateBudgetDto dto)
        {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

            var budget = new Budget
            {
                UserId = userId,
                StartDate = dto.StartDate,
                EndDate = dto.EndDate
            };

            _context.Budgets.Add(budget);
            await _context.SaveChangesAsync();

            return Ok(new { budgetId = budget.Id });
        }

        [HttpPost("{budgetId}/allocations")]
        public async Task<IActionResult> AddAllocation(int budgetId, [FromBody] BudgetAllocationDto dto)
        {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

            var budget = await _context.Budgets
                .FirstOrDefaultAsync(b => b.Id == budgetId && b.UserId == userId);
            if (budget == null) return NotFound();

            var incomes = await _context.IncomeSources.Where(i => i.UserId == userId && i.IsActive).ToListAsync();
            var mandatory = await _context.Expenses.Where(e => e.UserId == userId && e.IsMandatory).ToListAsync();
            var disposable = _calculator.CalculateDisposableIncome(incomes, mandatory, budget.SavingsGoalPercentage);

            var isUnderReview = _calculator.IsUnderReview(dto.Amount, disposable);
            var suggestedCap = isUnderReview ? _calculator.SuggestedCap(disposable) : (decimal?)null;

            var allocation = new BudgetAllocation
            {
                BudgetId = budgetId,
                CategoryId = dto.CategoryId,
                SubCategoryId = dto.SubCategoryId,
                Amount = dto.Amount,
                LocationTag = dto.LocationTag,
                IsUnderReview = isUnderReview,
                SuggestedCap = suggestedCap
            };

            _context.BudgetAllocations.Add(allocation);
            await _context.SaveChangesAsync();

            return Ok(new
            {
                allocationId = allocation.Id,
                isUnderReview,
                suggestedCap,
                verdict = _calculator.GetVerdict(dto.Amount, disposable)
            });
        }

        [HttpGet("{budgetId}/allocations")]
        public async Task<IActionResult> GetAllocations(int budgetId)
        {
            var allocations = await _context.BudgetAllocations
                .Where(a => a.BudgetId == budgetId)
                .Select(a => new
                {
                    a.Id,
                    a.Amount,
                    a.LocationTag,
                    a.IsUnderReview,
                    a.SuggestedCap,
                    CategoryName = a.Category.Name,
                    SubCategoryName = a.SubCategory != null ? a.SubCategory.Name : null
                })
                .ToListAsync();

            return Ok(allocations);
        }
    }
}