using EduWalletApp.Data;
using EduWalletApp.DTOs;
using EduWalletApp.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace EduWalletApp.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class TransactionController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public TransactionController(ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpPost]
        public async Task<IActionResult> AddTransaction([FromBody] CreateTransactionDto dto)
        {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

            var transaction = new Transaction
            {
                UserId = userId,
                BudgetAllocationId = dto.BudgetAllocationId,
                CategoryId = dto.CategoryId,
                SubCategoryId = dto.SubCategoryId,
                Amount = dto.Amount,
                Date = dto.Date,
                LocationTag = dto.LocationTag
            };

            _context.Transactions.Add(transaction);
            await _context.SaveChangesAsync();

            return Ok(new { transactionId = transaction.Id });
        }

        // Planned vs Actual — groups by budget allocation, comparing planned
        // Amount to the sum of actual transactions logged against it
        [HttpGet("variance/{budgetId}")]
        public async Task<IActionResult> GetVariance(int budgetId)
        {
            var allocations = await _context.BudgetAllocations
                .Where(a => a.BudgetId == budgetId)
                .Include(a => a.Transactions)
                .Select(a => new
                {
                    a.Id,
                    CategoryName = a.Category.Name,
                    Planned = a.Amount,
                    Actual = a.Transactions.Sum(t => t.Amount),
                    Variance = a.Amount - a.Transactions.Sum(t => t.Amount)
                })
                .ToListAsync();

            return Ok(allocations);
        }
    }
}