using EduWalletApp.Data;
using EduWalletApp.DTOs;
using EduWalletApp.Models;
using EduWalletApp.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace EduWalletApp.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class OnboardingController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<User> _userManager;
        private readonly ArchetypeCalculator _archetypeCalculator;

        public OnboardingController(
            ApplicationDbContext context,
            UserManager<User> userManager,
            ArchetypeCalculator archetypeCalculator)
        {
            _context = context;
            _userManager = userManager;
            _archetypeCalculator = archetypeCalculator;
        }

        [HttpPost("income")]
        [Authorize]
        public async Task<IActionResult> AddIncome([FromBody] List<IncomeSourceDto> incomes)
        {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

            foreach (var dto in incomes)
            {
                _context.IncomeSources.Add(new IncomeSource
                {
                    UserId = userId,
                    Source = dto.Source,
                    Amount = dto.Amount,
                    Frequency = dto.Frequency
                });
            }

            await _context.SaveChangesAsync();
            return Ok(new { message = "Income saved" });
        }

        [HttpPost("savings-goal")]
        [Authorize]
        public async Task<IActionResult> SetSavingsGoal([FromBody] SavingsGoalDto dto)
        {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

            var budget = await _context.Budgets
                .FirstOrDefaultAsync(b => b.UserId == userId);

            if (budget == null)
            {
                var now = DateOnly.FromDateTime(DateTime.UtcNow);
                budget = new Budget
                {
                    UserId = userId,
                    StartDate = new DateOnly(now.Year, now.Month, 1),
                    EndDate = new DateOnly(now.Year, now.Month, DateTime.DaysInMonth(now.Year, now.Month)),
                    SavingsGoalPercentage = dto.Percentage
                };
                _context.Budgets.Add(budget);
            }
            else
            {
                budget.SavingsGoalPercentage = dto.Percentage;
            }

            await _context.SaveChangesAsync();
            return Ok(new { message = "Savings goal set", percentage = dto.Percentage });
        }

        [HttpPost("quiz")]
        [Authorize]
        public async Task<IActionResult> SubmitQuiz([FromBody] QuizSubmissionDto dto)
        {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var user = await _context.Users.FindAsync(userId);

            if (user == null) return NotFound();

            var archetype = _archetypeCalculator.Calculate(dto.AnswerLeanings);
            user.Archetype = archetype;

            await _context.SaveChangesAsync();
            return Ok(new { archetype = archetype.ToString() });
        }

        [HttpPost("expenses")]
        [Authorize]
        public async Task<IActionResult> AddMandatoryExpenses([FromBody] List<ExpenseDto> expenses)
        {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

            foreach (var dto in expenses)
            {
                _context.Expenses.Add(new Expense
                {
                    UserId = userId,
                    Type = dto.Type,
                    Label = dto.Label,
                    Amount = dto.Amount,
                    Frequency = dto.Frequency,
                    IsMandatory = true
                });
            }

            var user = await _context.Users.FindAsync(userId);
            if (user != null) user.OnboardingComplete = true;

            await _context.SaveChangesAsync();
            return Ok(new { message = "Onboarding complete" });
        }
    }
}