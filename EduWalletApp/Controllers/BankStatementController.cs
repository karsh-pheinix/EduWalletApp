using EduWalletApp.Data;
using EduWalletApp.DTOs;
using EduWalletApp.Models;
using EduWalletApp.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using System.Globalization;
using System.Text.RegularExpressions;
using UglyToad.PdfPig;

namespace EduWalletApp.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class BankStatementController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _environment;
        private readonly IActivityLogger _activityLogger;
        private readonly ISpendingInsightService _insightService;

        private static readonly string[] AllowedExtensions = { ".csv", ".pdf" };
        private const long MaxFileSizeBytes = 5 * 1024 * 1024;

        public BankStatementController(
            ApplicationDbContext context,
            IWebHostEnvironment environment,
            IActivityLogger activityLogger,
            ISpendingInsightService insightService)
        {
            _context = context;
            _environment = environment;
            _activityLogger = activityLogger;
            _insightService = insightService;
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

            // Read the uploaded document before categorizing its transactions.
            var parsedRows = dto.FileType == StatementFileType.Csv
                ? ParseCsv(filePath, statement.Id)
                : ParsePdf(filePath, statement.Id);

            if (parsedRows.Count == 0)
            {
                _context.BankStatements.Remove(statement);
                await _context.SaveChangesAsync();
                System.IO.File.Delete(filePath);
                return BadRequest("No transactions could be read. Use a text-based PDF or a CSV containing date, description, and amount columns.");
            }

            var activeCategories = await _context.Categories
                .Include(c => c.SubCategories)
                .Where(c => c.IsActive)
                .ToListAsync();

            foreach (var row in parsedRows)
            {
                var (categoryId, subCategoryId) = StatementCategorizer.Match(row.Description, activeCategories);
                row.CategoryId = categoryId;
                row.SubCategoryId = subCategoryId;
            }

            _context.StatementTransactions.AddRange(parsedRows);
            await _context.SaveChangesAsync();
            await _activityLogger.LogAsync(userId, "UploadedBankStatement", $"statementId={statement.Id}");

            var insights = await _insightService.GetInsightsForStatementAsync(statement.Id, userId);

            return Ok(new
            {
                message = "Statement uploaded and processed",
                statementId = statement.Id,
                transactionsFound = parsedRows.Count,
                insights
            });
        }

        private List<StatementTransaction> ParseCsv(string filePath, int statementId)
        {
            var rows = new List<StatementTransaction>();
            var lines = System.IO.File.ReadAllLines(filePath)
                .Where(line => !string.IsNullOrWhiteSpace(line))
                .ToList();

            foreach (var line in lines)
            {
                var parts = SplitCsvLine(line);
                if (parts.Count < 3) continue;
                if (!TryFindDate(parts, out var date, out var dateIndex)) continue;
                if (!TryFindAmount(parts, out var amount, out var amountIndex)) continue;

                var description = string.Join(" ", parts
                    .Where((part, index) => index != dateIndex && index != amountIndex)
                    .Where(part => !string.IsNullOrWhiteSpace(part)))
                    .Trim();
                if (string.IsNullOrWhiteSpace(description) || IsStatementSummaryLine(description.ToLowerInvariant())) continue;

                rows.Add(new StatementTransaction
                {
                    BankStatementId = statementId,
                    Date = date,
                    Description = description,
                    Amount = amount
                });
            }

            return rows;
        }

        private List<StatementTransaction> ParsePdf(string filePath, int statementId)
        {
            var rows = new List<StatementTransaction>();
            using var document = PdfDocument.Open(filePath);
            foreach (var page in document.GetPages())
            {
                var words = page.GetWords()
                    .OrderByDescending(word => word.BoundingBox.Bottom)
                    .ThenBy(word => word.BoundingBox.Left)
                    .ToList();
                var lines = GroupWordsIntoLines(words).ToList();
                var inTransactionHistory = false;
                foreach (var line in lines)
                {
                    var lineText = line;
                    var normalizedLine = string.Join(" ", lineText).ToLowerInvariant();
                    if (normalizedLine.Contains("transaction history") || normalizedLine == "transactions")
                    {
                        inTransactionHistory = true;
                        continue;
                    }

                    if (!inTransactionHistory) continue;
                    if (IsStatementSummaryLine(normalizedLine)) break;
                    if (!TryFindPdfDate(lineText, out var date, out var dateIndex, out var dateLength)) continue;

                    // The last numeric column is often the running balance.
                    // The first currency value after the description is the
                    // transaction amount.
                    var amountIndex = line.FindIndex(dateIndex + dateLength, IsPdfAmount);
                    if (amountIndex < 0 || !TryParseAmount(line[amountIndex], out var amount)) continue;

                    var description = string.Join(" ", lineText.Skip(dateIndex + dateLength).Take(amountIndex - dateIndex - dateLength));
                    if (string.IsNullOrWhiteSpace(description) || IsStatementSummaryLine(description.ToLowerInvariant())) continue;

                    rows.Add(new StatementTransaction
                    {
                        BankStatementId = statementId,
                        Date = date,
                        Description = description,
                        Amount = amount
                    });
                }
            }

            return rows;
        }

        private static bool IsStatementSummaryLine(string line)
        {
            return line.Contains("opening balance")
                || line.Contains("closing balance")
                || line.Contains("available balance")
                || line.Contains("account summary")
                || line.StartsWith("total ")
                || line == "total";
        }

        private static IEnumerable<List<string>> GroupWordsIntoLines(IEnumerable<UglyToad.PdfPig.Content.Word> words)
        {
            var lines = new List<(double Bottom, List<string> Words)>();
            foreach (var word in words)
            {
                var line = lines.FirstOrDefault(candidate => Math.Abs(candidate.Bottom - word.BoundingBox.Bottom) <= 3);
                if (line.Words is null)
                {
                    lines.Add((word.BoundingBox.Bottom, new List<string> { word.Text }));
                }
                else
                {
                    line.Words.Add(word.Text);
                }
            }

            return lines.Select(line => line.Words);
        }

        private static bool IsPdfAmount(string value)
        {
            return value.Contains('.') && TryParseAmount(value, out _);
        }

        private static bool TryFindPdfDate(IReadOnlyList<string> words, out DateOnly date, out int startIndex, out int length)
        {
            for (var index = 0; index < words.Count; index++)
            {
                for (var count = 1; count <= Math.Min(3, words.Count - index); count++)
                {
                    var candidate = string.Join(" ", words.Skip(index).Take(count));
                    if (DateOnly.TryParse(candidate, CultureInfo.InvariantCulture, DateTimeStyles.None, out date))
                    {
                        startIndex = index;
                        length = count;
                        return true;
                    }
                }
            }

            date = default;
            startIndex = -1;
            length = 0;
            return false;
        }

        private static List<string> SplitCsvLine(string line)
        {
            var fields = new List<string>();
            var field = new System.Text.StringBuilder();
            var quoted = false;

            for (var index = 0; index < line.Length; index++)
            {
                var character = line[index];
                if (character == '"')
                {
                    if (quoted && index + 1 < line.Length && line[index + 1] == '"')
                    {
                        field.Append('"');
                        index++;
                    }
                    else quoted = !quoted;
                }
                else if (character == ',' && !quoted)
                {
                    fields.Add(field.ToString().Trim());
                    field.Clear();
                }
                else field.Append(character);
            }

            fields.Add(field.ToString().Trim());
            return fields;
        }

        private static bool TryFindDate(IReadOnlyList<string> parts, out DateOnly date, out int dateIndex)
        {
            for (dateIndex = 0; dateIndex < parts.Count; dateIndex++)
            {
                if (DateOnly.TryParse(parts[dateIndex], CultureInfo.InvariantCulture, DateTimeStyles.None, out date)) return true;
            }

            date = default;
            dateIndex = -1;
            return false;
        }

        private static bool TryFindAmount(IReadOnlyList<string> parts, out decimal amount, out int amountIndex)
        {
            for (amountIndex = parts.Count - 1; amountIndex >= 0; amountIndex--)
            {
                if (TryParseAmount(parts[amountIndex], out amount)) return true;
            }

            amount = default;
            amountIndex = -1;
            return false;
        }

        private static bool TryParseAmount(string value, out decimal amount)
        {
            var normalized = value.Trim().Replace("R", "", StringComparison.OrdinalIgnoreCase)
                .Replace(" ", "").Replace(",", "");
            var isParenthesized = normalized.StartsWith('(') && normalized.EndsWith(')');
            normalized = normalized.Trim('(', ')');

            if (decimal.TryParse(normalized, NumberStyles.AllowLeadingSign | NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out amount))
            {
                if (isParenthesized) amount = -Math.Abs(amount);
                return true;
            }

            amount = 0;
            return false;
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

        [HttpGet("{id}/insights")]
        public async Task<IActionResult> GetInsights(int id)
        {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

            var exists = await _context.BankStatements.AnyAsync(s => s.Id == id && s.UserId == userId);
            if (!exists) return NotFound();

            var insights = await _insightService.GetInsightsForStatementAsync(id, userId);
            return Ok(insights);
        }

        [HttpGet("transactions")]
        public async Task<IActionResult> GetAllStatementTransactions()
        {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

            var rows = await _context.StatementTransactions
                .Where(st => st.BankStatement.UserId == userId && st.Amount < 0)
                .OrderByDescending(st => st.Date)
                .Select(st => new
                {
                    st.Id,
                    Amount = -st.Amount,
                    st.Date,
                    LocationTag = st.Description,
                    CategoryName = st.Category != null ? st.Category.Name : "Uncategorized"
                })
                .ToListAsync();

            return Ok(rows);
        }
    }
}