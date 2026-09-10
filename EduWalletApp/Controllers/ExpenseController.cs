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
    public class ExpenseController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly IActivityLogger _activityLogger;
        private readonly IFinancialAlertService _alertService;

        public ExpenseController(ApplicationDbContext context, IActivityLogger activityLogger, IFinancialAlertService alertService)
        {
            _context = context;
            _activityLogger = activityLogger;
            _alertService = alertService;
        }

        private int CurrentUserId => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

        [HttpGet]
        public async Task<IActionResult> GetExpenses()
        {
            var expenses = await _context.Expenses
                .Where(e => e.UserId == CurrentUserId)
                .Where(e => e.EndDate == null || e.EndDate >= DateOnly.FromDateTime(DateTime.UtcNow))
                .Select(e => new
                {
                    e.Id,
                    e.Label,
                    e.Type,
                    e.Amount,
                    e.Frequency,
                    e.IsMandatory
                    ,e.StartDate
                    ,e.EndDate
                })
                .ToListAsync();

            return Ok(expenses);
        }

        [HttpPost]
        public async Task<IActionResult> CreateExpense([FromBody] ExpenseCrudDto dto)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);
            if (dto.EndDate.HasValue && dto.EndDate < dto.StartDate)
                return BadRequest("End date must be on or after the start date.");

            var expense = new Expense
            {
                UserId = CurrentUserId,
                Label = dto.Label,
                Type = dto.Type,
                Amount = dto.Amount,
                Frequency = dto.Frequency,
                IsMandatory = dto.IsMandatory
                ,StartDate = dto.StartDate
                ,EndDate = dto.EndDate
            };

            _context.Expenses.Add(expense);
            await _context.SaveChangesAsync();
            await _activityLogger.LogAsync(CurrentUserId, "CreatedExpense", $"amount={expense.Amount}");
            await _alertService.CheckAndCreateAlertsAsync(CurrentUserId);

            return Ok(new { expense.Id, expense.Label, expense.Type, expense.Amount, expense.Frequency, expense.IsMandatory, expense.StartDate, expense.EndDate });
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateExpense(int id, [FromBody] ExpenseCrudDto dto)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);
            if (dto.EndDate.HasValue && dto.EndDate < dto.StartDate)
                return BadRequest("End date must be on or after the start date.");

            var expense = await _context.Expenses
                .FirstOrDefaultAsync(e => e.Id == id && e.UserId == CurrentUserId);

            if (expense == null) return NotFound();

            expense.Label = dto.Label;
            expense.Type = dto.Type;
            expense.Amount = dto.Amount;
            expense.Frequency = dto.Frequency;
            expense.IsMandatory = dto.IsMandatory;
            expense.StartDate = dto.StartDate;
            expense.EndDate = dto.EndDate;

            await _context.SaveChangesAsync();

            return Ok(new { expense.Id, expense.Label, expense.Type, expense.Amount, expense.Frequency, expense.IsMandatory, expense.StartDate, expense.EndDate });
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