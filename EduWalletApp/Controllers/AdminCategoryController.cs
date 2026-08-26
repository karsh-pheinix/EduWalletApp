using EduWalletApp.Data;
using EduWalletApp.DTOs;
using EduWalletApp.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EduWalletApp.Controllers;

[ApiController]
[Route("api/admin/categories")]
[Authorize(Roles = "Admin")]
public class AdminCategoryController : ControllerBase
{
    private readonly ApplicationDbContext context;

    public AdminCategoryController(ApplicationDbContext context)
    {
        this.context = context;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll() => Ok(await context.Categories.AsNoTracking().Include(c => c.SubCategories).OrderBy(c => c.Name)
        .Select(c => new CategoryAdminDto(c.Id, c.Name, c.IsActive, c.SubCategories.Select(s => (object)new { s.Id, s.Name }))).ToListAsync());

    [HttpPost]
    public async Task<IActionResult> Create(CreateAdminCategoryDto dto)
    {
        if (!ModelState.IsValid) return ValidationProblem(ModelState);
        var name = dto.Name.Trim();
        if (await context.Categories.AnyAsync(c => c.Name == name)) return Conflict("A category with this name already exists.");
        var category = new Category { Name = name };
        context.Categories.Add(category);
        await context.SaveChangesAsync();
        return Ok(new { category.Id, category.Name, category.IsActive });
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, UpdateAdminCategoryDto dto)
    {
        if (!ModelState.IsValid) return ValidationProblem(ModelState);
        var category = await context.Categories.FindAsync(id);
        if (category is null) return NotFound();
        var name = dto.Name.Trim();
        if (await context.Categories.AnyAsync(c => c.Id != id && c.Name == name)) return Conflict("A category with this name already exists.");
        category.Name = name;
        category.IsActive = dto.IsActive;
        await context.SaveChangesAsync();
        return Ok(new { category.Id, category.Name, category.IsActive });
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        var category = await context.Categories.FindAsync(id);
        if (category is null) return NotFound();
        var inUse = await context.BudgetAllocations.AnyAsync(a => a.CategoryId == id) ||
            await context.Transactions.AnyAsync(t => t.CategoryId == id) ||
            await context.StatementTransactions.AnyAsync(t => t.CategoryId == id);
        if (inUse) return Conflict("This category is in use. Set it inactive instead of deleting.");
        context.Categories.Remove(category);
        await context.SaveChangesAsync();
        return NoContent();
    }
}