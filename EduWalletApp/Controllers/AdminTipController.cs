using EduWalletApp.Data;
using EduWalletApp.DTOs;
using EduWalletApp.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EduWalletApp.Controllers;

[ApiController]
[Route("api/admin/tips")]
[Authorize(Roles = "Admin")]
public class AdminTipController : ControllerBase
{
    private readonly ApplicationDbContext context;

    public AdminTipController(ApplicationDbContext context)
    {
        this.context = context;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll() => Ok(await context.FinancialTips.AsNoTracking().OrderByDescending(t => t.PublishedAt)
        .Select(t => new TipAdminDto(t.Id, t.Title, t.Content, t.Category, t.PublishedAt, t.TriggerType, t.ThresholdPercentage, t.TargetCategoryId)).ToListAsync());

    [HttpPost]
    public async Task<IActionResult> Create(CreateTipDto dto)
    {
        if (!ModelState.IsValid) return ValidationProblem(ModelState);
        var tip = new FinancialTip { Title = dto.Title.Trim(), Content = dto.Content.Trim(), Category = dto.Category, TriggerType = dto.TriggerType, ThresholdPercentage = dto.ThresholdPercentage, TargetCategoryId = dto.TargetCategoryId };
        context.FinancialTips.Add(tip);
        await context.SaveChangesAsync();
        return Ok(tip);
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, UpdateTipDto dto)
    {
        if (!ModelState.IsValid) return ValidationProblem(ModelState);
        var tip = await context.FinancialTips.FindAsync(id);
        if (tip is null) return NotFound();
        tip.Title = dto.Title.Trim(); tip.Content = dto.Content.Trim(); tip.Category = dto.Category; tip.TriggerType = dto.TriggerType; tip.ThresholdPercentage = dto.ThresholdPercentage; tip.TargetCategoryId = dto.TargetCategoryId;
        await context.SaveChangesAsync();
        return Ok(tip);
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        var tip = await context.FinancialTips.FindAsync(id);
        if (tip is null) return NotFound();
        context.FinancialTips.Remove(tip);
        await context.SaveChangesAsync();
        return NoContent();
    }
}