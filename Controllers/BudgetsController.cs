using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using PersonalFinanceTracker.Api.Data;
using PersonalFinanceTracker.Api.DTOs;
using PersonalFinanceTracker.Api.Models;

namespace PersonalFinanceTracker.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class BudgetsController : ControllerBase
    {
        private readonly AppDbContext _context;

        public BudgetsController(AppDbContext context)
        {
            _context = context;
        }

        private int GetUserId()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)
                ?? User.FindFirst("sub");

            return int.Parse(userIdClaim!.Value);
        }

        // Calculates how much has actually been spent in a category during a given month/year,
        // scoped to the logged-in user's accounts only.
        private async Task<decimal> CalculateAmountSpent(int userId, int categoryId, int month, int year)
        {
            return await _context.Transactions
                .Where(t => t.Account!.UserId == userId
                    && t.CategoryId == categoryId
                    && t.Type == "Expense"
                    && t.Date.Month == month
                    && t.Date.Year == year)
                .SumAsync(t => (decimal?)t.Amount) ?? 0;
        }

        // GET: api/budgets
        [HttpGet]
        public async Task<ActionResult<IEnumerable<BudgetResponse>>> GetBudgets([FromQuery] int? month, [FromQuery] int? year)
        {
            var userId = GetUserId();

            var query = _context.Budgets
                .Include(b => b.Category)
                .Where(b => b.UserId == userId);

            if (month.HasValue)
                query = query.Where(b => b.Month == month.Value);

            if (year.HasValue)
                query = query.Where(b => b.Year == year.Value);

            var budgets = await query.ToListAsync();

            var responses = new List<BudgetResponse>();
            foreach (var b in budgets)
            {
                var spent = await CalculateAmountSpent(userId, b.CategoryId, b.Month, b.Year);
                responses.Add(new BudgetResponse
                {
                    Id = b.Id,
                    CategoryId = b.CategoryId,
                    CategoryName = b.Category!.Name,
                    MonthlyLimit = b.MonthlyLimit,
                    Month = b.Month,
                    Year = b.Year,
                    AmountSpent = spent,
                    RemainingAmount = b.MonthlyLimit - spent
                });
            }

            return Ok(responses);
        }

        // GET: api/budgets/5
        [HttpGet("{id}")]
        public async Task<ActionResult<BudgetResponse>> GetBudget(int id)
        {
            var userId = GetUserId();

            var budget = await _context.Budgets
                .Include(b => b.Category)
                .FirstOrDefaultAsync(b => b.Id == id && b.UserId == userId);

            if (budget == null)
                return NotFound();

            var spent = await CalculateAmountSpent(userId, budget.CategoryId, budget.Month, budget.Year);

            return Ok(new BudgetResponse
            {
                Id = budget.Id,
                CategoryId = budget.CategoryId,
                CategoryName = budget.Category!.Name,
                MonthlyLimit = budget.MonthlyLimit,
                Month = budget.Month,
                Year = budget.Year,
                AmountSpent = spent,
                RemainingAmount = budget.MonthlyLimit - spent
            });
        }

        // POST: api/budgets
        [HttpPost]
        public async Task<ActionResult<BudgetResponse>> CreateBudget(BudgetRequest request)
        {
            var userId = GetUserId();

            var categoryExists = await _context.Categories.AnyAsync(c => c.Id == request.CategoryId);
            if (!categoryExists)
                return BadRequest(new { message = "Invalid category." });

            // Prevent duplicate budgets for the same category/month/year
            var duplicate = await _context.Budgets.AnyAsync(b =>
                b.UserId == userId &&
                b.CategoryId == request.CategoryId &&
                b.Month == request.Month &&
                b.Year == request.Year);

            if (duplicate)
                return Conflict(new { message = "A budget for this category and month already exists." });

            var budget = new Budget
            {
                UserId = userId,
                CategoryId = request.CategoryId,
                MonthlyLimit = request.MonthlyLimit,
                Month = request.Month,
                Year = request.Year
            };

            _context.Budgets.Add(budget);
            await _context.SaveChangesAsync();

            var created = await _context.Budgets
                .Include(b => b.Category)
                .FirstAsync(b => b.Id == budget.Id);

            var spent = await CalculateAmountSpent(userId, created.CategoryId, created.Month, created.Year);

            var response = new BudgetResponse
            {
                Id = created.Id,
                CategoryId = created.CategoryId,
                CategoryName = created.Category!.Name,
                MonthlyLimit = created.MonthlyLimit,
                Month = created.Month,
                Year = created.Year,
                AmountSpent = spent,
                RemainingAmount = created.MonthlyLimit - spent
            };

            return CreatedAtAction(nameof(GetBudget), new { id = budget.Id }, response);
        }

        // PUT: api/budgets/5
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateBudget(int id, BudgetRequest request)
        {
            var userId = GetUserId();

            var budget = await _context.Budgets
                .FirstOrDefaultAsync(b => b.Id == id && b.UserId == userId);

            if (budget == null)
                return NotFound();

            var categoryExists = await _context.Categories.AnyAsync(c => c.Id == request.CategoryId);
            if (!categoryExists)
                return BadRequest(new { message = "Invalid category." });

            budget.CategoryId = request.CategoryId;
            budget.MonthlyLimit = request.MonthlyLimit;
            budget.Month = request.Month;
            budget.Year = request.Year;

            await _context.SaveChangesAsync();

            return NoContent();
        }

        // DELETE: api/budgets/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteBudget(int id)
        {
            var userId = GetUserId();

            var budget = await _context.Budgets
                .FirstOrDefaultAsync(b => b.Id == id && b.UserId == userId);

            if (budget == null)
                return NotFound();

            _context.Budgets.Remove(budget);
            await _context.SaveChangesAsync();

            return NoContent();
        }
    }
}