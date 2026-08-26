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
        private readonly IActivityLogger _activityLogger;
        private readonly IFinancialAlertService _alertService;

        public BudgetController(ApplicationDbContext context, FinanceCalculator calculator, IActivityLogger activityLogger, IFinancialAlertService alertService)
        {
            _context = context;
            _calculator = calculator;
            _activityLogger = activityLogger;
            _alertService = alertService;
        }

        private int CurrentUserId => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

        // ---------- Budgets ----------

        [HttpPost]
        public async Task<IActionResult> CreateBudget([FromBody] CreateBudgetDto dto)
        {
            var budget = new Budget
            {
                UserId = CurrentUserId,
                StartDate = dto.StartDate,
                EndDate = dto.EndDate
            };

            _context.Budgets.Add(budget);
            await _context.SaveChangesAsync();
            await _activityLogger.LogAsync(CurrentUserId, "CreatedBudget", $"budgetId={budget.Id}");
            await _alertService.CheckAndCreateAlertsAsync(CurrentUserId);

            return Ok(new { budgetId = budget.Id });
        }

        [HttpGet]
        public async Task<IActionResult> GetBudgets()
        {
            var budgets = await _context.Budgets
                .Where(b => b.UserId == CurrentUserId)
                .OrderByDescending(b => b.StartDate)
                .Select(b => new
                {
                    b.Id,
                    b.StartDate,
                    b.EndDate,
                    b.SavingsGoalPercentage
                })
                .ToListAsync();

            return Ok(budgets);
        }

        [HttpGet("{budgetId}")]
        public async Task<IActionResult> GetBudget(int budgetId)
        {
            var budget = await _context.Budgets
                .Where(b => b.Id == budgetId && b.UserId == CurrentUserId)
                .Select(b => new
                {
                    b.Id,
                    b.StartDate,
                    b.EndDate,
                    b.SavingsGoalPercentage
                })
                .FirstOrDefaultAsync();

            if (budget == null) return NotFound();

            return Ok(budget);
        }

        [HttpPut("{budgetId}")]
        public async Task<IActionResult> UpdateBudget(int budgetId, [FromBody] UpdateBudgetDto dto)
        {
            var budget = await _context.Budgets
                .FirstOrDefaultAsync(b => b.Id == budgetId && b.UserId == CurrentUserId);

            if (budget == null) return NotFound();

            budget.StartDate = dto.StartDate;
            budget.EndDate = dto.EndDate;
            budget.SavingsGoalPercentage = dto.SavingsGoalPercentage;

            await _context.SaveChangesAsync();

            return Ok(new
            {
                budget.Id,
                budget.StartDate,
                budget.EndDate,
                budget.SavingsGoalPercentage
            });
        }

        [HttpDelete("{budgetId}")]
        public async Task<IActionResult> DeleteBudget(int budgetId)
        {
            var budget = await _context.Budgets
                .Include(b => b.BudgetAllocations)
                .FirstOrDefaultAsync(b => b.Id == budgetId && b.UserId == CurrentUserId);

            if (budget == null) return NotFound();

            _context.BudgetAllocations.RemoveRange(budget.BudgetAllocations);
            _context.Budgets.Remove(budget);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        // ---------- Allocations ----------

        [HttpPost("{budgetId}/allocations")]
        public async Task<IActionResult> AddAllocation(int budgetId, [FromBody] BudgetAllocationDto dto)
        {
            var budget = await _context.Budgets
                .FirstOrDefaultAsync(b => b.Id == budgetId && b.UserId == CurrentUserId);
            if (budget == null) return NotFound();

            var incomes = await _context.IncomeSources.Where(i => i.UserId == CurrentUserId && i.IsActive).ToListAsync();
            var mandatory = await _context.Expenses.Where(e => e.UserId == CurrentUserId && e.IsMandatory).ToListAsync();
            var disposable = _calculator.CalculateDisposableIncome(incomes, mandatory, budget.SavingsGoalPercentage);

            var isUnderReview = _calculator.IsUnderReview(dto.Amount, disposable);
            var suggestedCap = isUnderReview ? _calculator.SuggestedCap(disposable) : (decimal?)null;

            var allocation = new BudgetAllocation
            {
                BudgetId = budgetId,
                CategoryId = dto.CategoryId,
                SubCategoryId = dto.SubCategoryId,
                BudgetedAmount = dto.Amount,
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
            var budgetExists = await _context.Budgets
                .AnyAsync(b => b.Id == budgetId && b.UserId == CurrentUserId);
            if (!budgetExists) return NotFound();

            var allocations = await _context.BudgetAllocations
                .Where(a => a.BudgetId == budgetId)
                .Select(a => new
                {
                    a.Id,
                    BudgetedAmount = a.BudgetedAmount,
                    a.LocationTag,
                    a.IsUnderReview,
                    a.SuggestedCap,
                    a.CategoryId,
                    a.SubCategoryId,
                    CategoryName = a.Category.Name,
                    SubCategoryName = a.SubCategory != null ? a.SubCategory.Name : null
                })
                .ToListAsync();

            return Ok(allocations);
        }

        [HttpGet("{budgetId}/allocations/{allocationId}")]
        public async Task<IActionResult> GetAllocation(int budgetId, int allocationId)
        {
            var budgetExists = await _context.Budgets
                .AnyAsync(b => b.Id == budgetId && b.UserId == CurrentUserId);
            if (!budgetExists) return NotFound();

            var allocation = await _context.BudgetAllocations
                .Where(a => a.Id == allocationId && a.BudgetId == budgetId)
                .Select(a => new
                {
                    a.Id,
                    BudgetedAmount = a.BudgetedAmount,
                    a.LocationTag,
                    a.IsUnderReview,
                    a.SuggestedCap,
                    a.CategoryId,
                    a.SubCategoryId,
                    CategoryName = a.Category.Name,
                    SubCategoryName = a.SubCategory != null ? a.SubCategory.Name : null
                })
                .FirstOrDefaultAsync();

            if (allocation == null) return NotFound();

            return Ok(allocation);
        }

        [HttpPut("{budgetId}/allocations/{allocationId}")]
        public async Task<IActionResult> UpdateAllocation(int budgetId, int allocationId, [FromBody] UpdateBudgetAllocationDto dto)
        {
            var budget = await _context.Budgets
                .FirstOrDefaultAsync(b => b.Id == budgetId && b.UserId == CurrentUserId);
            if (budget == null) return NotFound();

            var allocation = await _context.BudgetAllocations
                .FirstOrDefaultAsync(a => a.Id == allocationId && a.BudgetId == budgetId);
            if (allocation == null) return NotFound();

            var categoryExists = await _context.Categories
                .AnyAsync(category => category.Id == dto.CategoryId && category.IsActive);
            if (!categoryExists) return BadRequest("The selected category is invalid.");

            if (dto.SubCategoryId.HasValue)
            {
                var subCategoryMatches = await _context.SubCategories
                    .AnyAsync(subCategory => subCategory.Id == dto.SubCategoryId.Value && subCategory.CategoryId == dto.CategoryId);
                if (!subCategoryMatches) return BadRequest("The selected subcategory is invalid.");
            }

            var incomes = await _context.IncomeSources.Where(i => i.UserId == CurrentUserId && i.IsActive).ToListAsync();
            var mandatory = await _context.Expenses.Where(e => e.UserId == CurrentUserId && e.IsMandatory).ToListAsync();
            var disposable = _calculator.CalculateDisposableIncome(incomes, mandatory, budget.SavingsGoalPercentage);

            var isUnderReview = _calculator.IsUnderReview(dto.Amount, disposable);
            var suggestedCap = isUnderReview ? _calculator.SuggestedCap(disposable) : (decimal?)null;

            allocation.CategoryId = dto.CategoryId;
            allocation.SubCategoryId = dto.SubCategoryId;
            allocation.BudgetedAmount = dto.Amount;
            allocation.LocationTag = dto.LocationTag;
            allocation.IsUnderReview = isUnderReview;
            allocation.SuggestedCap = suggestedCap;

            await _context.SaveChangesAsync();

            return Ok(new
            {
                allocation.Id,
                isUnderReview,
                suggestedCap,
                verdict = _calculator.GetVerdict(dto.Amount, disposable)
            });
        }

        [HttpDelete("{budgetId}/allocations/{allocationId}")]
        public async Task<IActionResult> DeleteAllocation(int budgetId, int allocationId)
        {
            var budgetExists = await _context.Budgets
                .AnyAsync(b => b.Id == budgetId && b.UserId == CurrentUserId);
            if (!budgetExists) return NotFound();

            var allocation = await _context.BudgetAllocations
                .Include(a => a.Transactions)
                .FirstOrDefaultAsync(a => a.Id == allocationId && a.BudgetId == budgetId);
            if (allocation == null) return NotFound();

            if (allocation.Transactions.Any())
            {
                _context.Transactions.RemoveRange(allocation.Transactions);
            }

            _context.BudgetAllocations.Remove(allocation);
            await _context.SaveChangesAsync();

            return NoContent();
        }
    }
}