using EduWalletApp.Data;
using EduWalletApp.DTOs;
using EduWalletApp.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EduWalletApp.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class SubCategoryController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public SubCategoryController(ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpPost]
        public async Task<IActionResult> CreateSubCategory([FromBody] SubCategoryCrudDto dto)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            var categoryExists = await _context.Categories.AnyAsync(c => c.Id == dto.CategoryId);
            if (!categoryExists) return NotFound(new { message = "Category not found." });

            var subCategory = new SubCategory
            {
                Name = dto.Name,
                CategoryId = dto.CategoryId
            };

            _context.SubCategories.Add(subCategory);
            await _context.SaveChangesAsync();

            return Ok(new { subCategory.Id, subCategory.Name, subCategory.CategoryId });
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateSubCategory(int id, [FromBody] SubCategoryCrudDto dto)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            var subCategory = await _context.SubCategories.FindAsync(id);
            if (subCategory == null) return NotFound();

            subCategory.Name = dto.Name;
            subCategory.CategoryId = dto.CategoryId;

            await _context.SaveChangesAsync();

            return Ok(new { subCategory.Id, subCategory.Name, subCategory.CategoryId });
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteSubCategory(int id)
        {
            var subCategory = await _context.SubCategories.FindAsync(id);
            if (subCategory == null) return NotFound();

            _context.SubCategories.Remove(subCategory);
            await _context.SaveChangesAsync();

            return NoContent();
        }
    }
}