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
    public class BankStatementController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _environment;

        private static readonly string[] AllowedExtensions = { ".csv", ".pdf" };
        private const long MaxFileSizeBytes = 5 * 1024 * 1024;

        public BankStatementController(ApplicationDbContext context, IWebHostEnvironment environment)
        {
            _context = context;
            _environment = environment;
        }

        [HttpPost("upload")]
        public async Task<IActionResult> Upload([FromForm] BankStatementUploadDto dto)
        {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

            if (dto.File == null || dto.File.Length == 0)
                return BadRequest("No file was uploaded.");

            if (dto.File.Length > MaxFileSizeBytes)
                return BadRequest("File exceeds the 5MB size limit.");

            var extension = Path.GetExtension(dto.File.FileName).ToLowerInvariant();
            if (!AllowedExtensions.Contains(extension))
                return BadRequest("Only .csv and .pdf files are allowed.");

            var uploadsFolder = Path.Combine(_environment.WebRootPath ?? "wwwroot", "statements");
            Directory.CreateDirectory(uploadsFolder);

            var uniqueFileName = $"{Guid.NewGuid()}{extension}";
            var filePath = Path.Combine(uploadsFolder, uniqueFileName);

            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await dto.File.CopyToAsync(stream);
            }

            var statement = new BankStatement
            {
                UserId = userId,
                AccountId = dto.AccountId,
                FileName = dto.File.FileName,
                FileType = dto.FileType
            };

            _context.BankStatements.Add(statement);
            await _context.SaveChangesAsync();

            // Simulated parsing: CSV gets a genuine lightweight parse (real
            // dynamic data); PDF generates plausible placeholder rows, since
            // real PDF parsing is explicitly simulated per the spec.
            var parsedRows = dto.FileType == StatementFileType.Csv
                ? ParseCsv(filePath, statement.Id)
                : GenerateSimulatedRows(statement.Id);

            _context.StatementTransactions.AddRange(parsedRows);
            await _context.SaveChangesAsync();

            return Ok(new
            {
                message = "Statement uploaded and processed",
                statementId = statement.Id,
                transactionsFound = parsedRows.Count
            });
        }

        private List<StatementTransaction> ParseCsv(string filePath, int statementId)
        {
            var rows = new List<StatementTransaction>();
            var lines = System.IO.File.ReadAllLines(filePath);

            // Expects: Date,Description,Amount — skips header row
            foreach (var line in lines.Skip(1))
            {
                var parts = line.Split(',');
                if (parts.Length < 3) continue;

                if (DateOnly.TryParse(parts[0], out var date) &&
                    decimal.TryParse(parts[2], out var amount))
                {
                    rows.Add(new StatementTransaction
                    {
                        BankStatementId = statementId,
                        Date = date,
                        Description = parts[1],
                        Amount = amount
                    });
                }
            }

            return rows;
        }

        private List<StatementTransaction> GenerateSimulatedRows(int statementId)
        {
            // PDF parsing is simulated per spec — generates a few plausible
            // placeholder rows rather than performing real text extraction
            var random = new Random();
            var descriptions = new[] { "Campus Cafeteria", "Uber", "Bookstore", "Airtime" };

            return Enumerable.Range(0, 4).Select(i => new StatementTransaction
            {
                BankStatementId = statementId,
                Date = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-i)),
                Description = descriptions[random.Next(descriptions.Length)],
                Amount = Math.Round((decimal)(random.NextDouble() * 200 + 20), 2)
            }).ToList();
        }
        [HttpGet]
        public async Task<IActionResult> GetStatements()
        {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

            var statements = await _context.BankStatements
                .Where(s => s.UserId == userId)
                .OrderByDescending(s => s.UploadedAt)
                .Select(s => new
                {
                    s.Id,
                    s.FileName,
                    s.FileType,
                    s.UploadedAt,
                    TransactionCount = s.StatementTransactions.Count()
                })
                .ToListAsync();

            return Ok(statements);
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteStatement(int id)
        {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

            var statement = await _context.BankStatements
                .Include(s => s.StatementTransactions)
                .FirstOrDefaultAsync(s => s.Id == id && s.UserId == userId);

            if (statement == null) return NotFound();

            _context.StatementTransactions.RemoveRange(statement.StatementTransactions);
            _context.BankStatements.Remove(statement);
            await _context.SaveChangesAsync();

            return NoContent();
        }
    }
}