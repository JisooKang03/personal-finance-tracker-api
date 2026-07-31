using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PersonalFinanceTracker.Api.Data;
using PersonalFinanceTracker.Api.DTOs;
using PersonalFinanceTracker.Api.Models;

namespace PersonalFinanceTracker.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize] // Any logged-in user can access this controller by default
    public class CategoriesController : ControllerBase
    {
        private readonly AppDbContext _context;

        public CategoriesController(AppDbContext context)
        {
            _context = context;
        }

        // GET: api/categories
        // Any authenticated user (User or Admin) can view categories
        [HttpGet]
        public async Task<ActionResult<IEnumerable<CategoryResponse>>> GetCategories()
        {
            var categories = await _context.Categories
                .Select(c => new CategoryResponse
                {
                    Id = c.Id,
                    Name = c.Name,
                    Type = c.Type
                })
                .ToListAsync();

            return Ok(categories);
        }

        // GET: api/categories/5
        [HttpGet("{id}")]
        public async Task<ActionResult<CategoryResponse>> GetCategory(int id)
        {
            var category = await _context.Categories
                .Where(c => c.Id == id)
                .Select(c => new CategoryResponse
                {
                    Id = c.Id,
                    Name = c.Name,
                    Type = c.Type
                })
                .FirstOrDefaultAsync();

            if (category == null)
                return NotFound();

            return Ok(category);
        }

        // POST: api/categories
        // Only Admins can create new categories
        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<CategoryResponse>> CreateCategory(CategoryRequest request)
        {
            var duplicate = await _context.Categories
                .AnyAsync(c => c.Name.ToLower() == request.Name.ToLower());

            if (duplicate)
                return Conflict(new { message = "A category with this name already exists." });

            var category = new Category
            {
                Name = request.Name,
                Type = request.Type
            };

            _context.Categories.Add(category);
            await _context.SaveChangesAsync();

            var response = new CategoryResponse
            {
                Id = category.Id,
                Name = category.Name,
                Type = category.Type
            };

            return CreatedAtAction(nameof(GetCategory), new { id = category.Id }, response);
        }

        // PUT: api/categories/5
        // Only Admins can edit categories
        [HttpPut("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> UpdateCategory(int id, CategoryRequest request)
        {
            var category = await _context.Categories.FindAsync(id);

            if (category == null)
                return NotFound();

            category.Name = request.Name;
            category.Type = request.Type;

            await _context.SaveChangesAsync();

            return NoContent();
        }

        // DELETE: api/categories/5
        // Only Admins can delete categories
        [HttpDelete("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteCategory(int id)
        {
            var category = await _context.Categories.FindAsync(id);

            if (category == null)
                return NotFound();

            // Prevent deleting a category that's actively in use
            var inUse = await _context.Transactions.AnyAsync(t => t.CategoryId == id) ||
                        await _context.Budgets.AnyAsync(b => b.CategoryId == id);

            if (inUse)
                return Conflict(new { message = "Cannot delete a category that is currently in use by transactions or budgets." });

            _context.Categories.Remove(category);
            await _context.SaveChangesAsync();

            return NoContent();
        }
    }
}