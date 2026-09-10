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
    [Route("api/IncomeSources")]
    [Authorize]
    public class IncomeSourceController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public IncomeSourceController(ApplicationDbContext context)
        {
            _context = context;
        }

        private int CurrentUserId => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

        [HttpGet]
        public async Task<IActionResult> GetIncomeSources()
        {
            var sources = await _context.IncomeSources
                .Where(i => i.UserId == CurrentUserId && i.IsActive)
                .Where(i => i.EndDate == null || i.EndDate >= DateOnly.FromDateTime(DateTime.UtcNow))
                .Select(i => new
                {
                    i.Id,
                    i.Source,
                    i.Amount,
                    i.Frequency
                    ,i.StartDate
                    ,i.EndDate
                })
                .ToListAsync();

            return Ok(sources);
        }

        [HttpPost]
        public async Task<IActionResult> CreateIncomeSource([FromBody] IncomeSourceCrudDto dto)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);
            if (dto.EndDate.HasValue && dto.EndDate < dto.StartDate)
                return BadRequest("End date must be on or after the start date.");

            var source = new IncomeSource
            {
                UserId = CurrentUserId,
                Source = dto.Source,
                Amount = dto.Amount,
                Frequency = dto.Frequency,
                StartDate = dto.StartDate,
                EndDate = dto.EndDate,
                IsActive = true
            };

            _context.IncomeSources.Add(source);
            await _context.SaveChangesAsync();

            return Ok(new { source.Id, source.Source, source.Amount, source.Frequency, source.StartDate, source.EndDate });
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateIncomeSource(int id, [FromBody] IncomeSourceCrudDto dto)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);
            if (dto.EndDate.HasValue && dto.EndDate < dto.StartDate)
                return BadRequest("End date must be on or after the start date.");

            var source = await _context.IncomeSources
                .FirstOrDefaultAsync(i => i.Id == id && i.UserId == CurrentUserId);

            if (source == null) return NotFound();

            source.Source = dto.Source;
            source.Amount = dto.Amount;
            source.Frequency = dto.Frequency;
            source.StartDate = dto.StartDate;
            source.EndDate = dto.EndDate;

            await _context.SaveChangesAsync();

            return Ok(new { source.Id, source.Source, source.Amount, source.Frequency, source.StartDate, source.EndDate });
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteIncomeSource(int id)
        {
            var source = await _context.IncomeSources
                .FirstOrDefaultAsync(i => i.Id == id && i.UserId == CurrentUserId);

            if (source == null) return NotFound();

            // Soft delete keeps historical reports/calculations intact
            source.IsActive = false;
            await _context.SaveChangesAsync();

            return NoContent();
        }
    }
}