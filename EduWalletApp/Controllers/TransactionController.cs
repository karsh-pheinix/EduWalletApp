using EduWalletApp.Data;
using EduWalletApp.DTOs;
using EduWalletApp.Models;
using EduWalletApp.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using System.Data;

namespace EduWalletApp.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class TransactionController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly IActivityLogger _activityLogger;
        private readonly IFinancialAlertService _alertService;

        public TransactionController(ApplicationDbContext context, IActivityLogger activityLogger, IFinancialAlertService alertService)
        {
            _context = context;
            _activityLogger = activityLogger;
            _alertService = alertService;
        }

        [HttpPost]
        public async Task<IActionResult> AddTransaction([FromBody] CreateTransactionDto dto)
        {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

            if (dto.BudgetAllocationId.HasValue)
            {
                var allocation = await _context.BudgetAllocations
                    .Where(a => a.Id == dto.BudgetAllocationId.Value && a.Budget.UserId == userId)
                    .Select(a => new
                    {
                        a.CategoryId,
                        a.SubCategoryId,
                        CategoryIsActive = a.Category.IsActive,
                        BudgetStartDate = a.Budget.StartDate,
                        BudgetEndDate = a.Budget.EndDate
                    })
                    .FirstOrDefaultAsync();
                if (allocation == null) return BadRequest("The selected budget allocation is invalid.");
                if (!allocation.CategoryIsActive) return BadRequest("The selected budget allocation uses an inactive category.");
                if (dto.Date < allocation.BudgetStartDate || dto.Date > allocation.BudgetEndDate)
                {
                    return BadRequest($"The transaction date must be between {allocation.BudgetStartDate:yyyy-MM-dd} and {allocation.BudgetEndDate:yyyy-MM-dd}.");
                }

                dto.CategoryId = allocation.CategoryId;
                dto.SubCategoryId = allocation.SubCategoryId;
            }
            else if (!await _context.Categories.AnyAsync(category => category.Id == dto.CategoryId && category.IsActive))
            {
                return BadRequest("The selected category is invalid.");
            }

            if (dto.SubCategoryId.HasValue &&
                !await _context.SubCategories.AnyAsync(subCategory =>
                    subCategory.Id == dto.SubCategoryId.Value && subCategory.CategoryId == dto.CategoryId))
            {
                return BadRequest("The selected subcategory does not belong to the selected category.");
            }

            var normalizedLocation = dto.LocationTag?.Trim();
            await using var transactionScope = await _context.Database.BeginTransactionAsync(IsolationLevel.Serializable);

            var duplicateExists = await _context.Transactions.AnyAsync(existing =>
                existing.UserId == userId &&
                existing.CategoryId == dto.CategoryId &&
                existing.SubCategoryId == dto.SubCategoryId &&
                existing.BudgetAllocationId == dto.BudgetAllocationId &&
                existing.Amount == dto.Amount &&
                existing.Date == dto.Date &&
                (existing.LocationTag ?? string.Empty) == (normalizedLocation ?? string.Empty));
            if (duplicateExists)
            {
                return Conflict("An identical transaction already exists.");
            }

            var transaction = new Transaction
            {
                UserId = userId,
                BudgetAllocationId = dto.BudgetAllocationId,
                CategoryId = dto.CategoryId,
                SubCategoryId = dto.SubCategoryId,
                Amount = dto.Amount,
                Date = dto.Date,
                LocationTag = normalizedLocation
            };

            _context.Transactions.Add(transaction);
            await _context.SaveChangesAsync();
            await transactionScope.CommitAsync();
            await _activityLogger.LogAsync(userId, "CreatedTransaction", $"amount={transaction.Amount}");
            await _alertService.CheckAndCreateAlertsAsync(userId);

            return Ok(new { transactionId = transaction.Id });
        }

        // Planned vs Actual — groups by budget allocation, comparing planned
        // Amount to the sum of actual transactions logged against it
        [HttpGet("variance/{budgetId}")]
        public async Task<IActionResult> GetVariance(int budgetId)
        {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

            var allocations = await _context.BudgetAllocations
                .Where(a => a.BudgetId == budgetId && a.Budget.UserId == userId)
                .Include(a => a.Transactions)
                .Select(a => new
                {
                    a.Id,
                    a.CategoryId,
                    a.SubCategoryId,
                    CategoryName = a.Category.Name,
                    Planned = a.BudgetedAmount,
                    Actual = a.Transactions
                        .Where(t => t.Date >= a.Budget.StartDate && t.Date <= a.Budget.EndDate)
                        .Sum(t => t.Amount),
                    Variance = a.BudgetedAmount - a.Transactions
                        .Where(t => t.Date >= a.Budget.StartDate && t.Date <= a.Budget.EndDate)
                        .Sum(t => t.Amount)
                })
                .ToListAsync();

            return Ok(allocations);
        }
        [HttpGet]
        public async Task<IActionResult> GetTransactions([FromQuery] int? budgetId = null)
        {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

            var query = _context.Transactions.Where(t => t.UserId == userId);

            if (budgetId.HasValue)
            {
                query = query.Where(t => t.BudgetAllocationId != null
                    && t.BudgetAllocation!.BudgetId == budgetId.Value
                    && t.Date >= t.BudgetAllocation.Budget.StartDate
                    && t.Date <= t.BudgetAllocation.Budget.EndDate);
            }

            var transactions = await query
                .OrderByDescending(t => t.Date)
                .Select(t => new
                {
                    t.Id,
                    t.Amount,
                    t.Date,
                    t.LocationTag,
                    CategoryName = t.Category.Name,
                    SubCategoryName = t.SubCategory != null ? t.SubCategory.Name : null
                })
                .ToListAsync();

            return Ok(transactions);
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteTransaction(int id)
        {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

            var transaction = await _context.Transactions
                .FirstOrDefaultAsync(t => t.Id == id && t.UserId == userId);

            if (transaction == null) return NotFound();

            _context.Transactions.Remove(transaction);
            await _context.SaveChangesAsync();

            return NoContent();
        }
    }
}