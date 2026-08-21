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
    public class ExpenseController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public ExpenseController(ApplicationDbContext context)
        {
            _context = context;
        }

        private int CurrentUserId => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

        [HttpGet]
        public async Task<IActionResult> GetExpenses()
        {
            var expenses = await _context.Expenses
                .Where(e => e.UserId == CurrentUserId)
                .Select(e => new
                {
                    e.Id,
                    e.Label,
                    e.Type,
                    e.Amount,
                    e.Frequency,
                    e.IsMandatory
                })
                .ToListAsync();

            return Ok(expenses);
        }

        [HttpPost]
        public async Task<IActionResult> CreateExpense([FromBody] ExpenseCrudDto dto)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            var expense = new Expense
            {
                UserId = CurrentUserId,
                Label = dto.Label,
                Type = dto.Type,
                Amount = dto.Amount,
                Frequency = dto.Frequency,
                IsMandatory = dto.IsMandatory
            };

            _context.Expenses.Add(expense);
            await _context.SaveChangesAsync();

            return Ok(new { expense.Id, expense.Label, expense.Type, expense.Amount, expense.Frequency, expense.IsMandatory });
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateExpense(int id, [FromBody] ExpenseCrudDto dto)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            var expense = await _context.Expenses
                .FirstOrDefaultAsync(e => e.Id == id && e.UserId == CurrentUserId);

            if (expense == null) return NotFound();

            expense.Label = dto.Label;
            expense.Type = dto.Type;
            expense.Amount = dto.Amount;
            expense.Frequency = dto.Frequency;
            expense.IsMandatory = dto.IsMandatory;

            await _context.SaveChangesAsync();

            return Ok(new { expense.Id, expense.Label, expense.Type, expense.Amount, expense.Frequency, expense.IsMandatory });
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteExpense(int id)
        {
            var expense = await _context.Expenses
                .FirstOrDefaultAsync(e => e.Id == id && e.UserId == CurrentUserId);

            if (expense == null) return NotFound();

            _context.Expenses.Remove(expense);
            await _context.SaveChangesAsync();

            return NoContent();
        }
    }
}