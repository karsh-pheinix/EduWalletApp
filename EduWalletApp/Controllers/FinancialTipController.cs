using EduWalletApp.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EduWalletApp.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class FinancialTipController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public FinancialTipController(ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<IActionResult> GetTips()
        {
            var tips = await _context.FinancialTips
                .OrderByDescending(t => t.PublishedAt)
                .ToListAsync();

            return Ok(tips);
        }
    }
}