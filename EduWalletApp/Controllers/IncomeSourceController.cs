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
                .Select(i => new
                {
                    i.Id,
                    i.Source,
                    i.Amount,
                    i.Frequency
                })
                .ToListAsync();

            return Ok(sources);
        }

        [HttpPost]
        public async Task<IActionResult> CreateIncomeSource([FromBody] IncomeSourceCrudDto dto)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            var source = new IncomeSource
            {
                UserId = CurrentUserId,
                Source = dto.Source,
                Amount = dto.Amount,
                Frequency = dto.Frequency,
                IsActive = true
            };

            _context.IncomeSources.Add(source);
            await _context.SaveChangesAsync();

            return Ok(new { source.Id, source.Source, source.Amount, source.Frequency });
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateIncomeSource(int id, [FromBody] IncomeSourceCrudDto dto)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            var source = await _context.IncomeSources
                .FirstOrDefaultAsync(i => i.Id == id && i.UserId == CurrentUserId);

            if (source == null) return NotFound();

            source.Source = dto.Source;
            source.Amount = dto.Amount;
            source.Frequency = dto.Frequency;

            await _context.SaveChangesAsync();

            return Ok(new { source.Id, source.Source, source.Amount, source.Frequency });
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