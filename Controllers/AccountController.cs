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
    public class AccountsController : ControllerBase
    {
        private readonly AppDbContext _context;

        public AccountsController(AppDbContext context)
        {
            _context = context;
        }

        private int GetUserId()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)
                ?? User.FindFirst("sub");

            return int.Parse(userIdClaim!.Value);
        }

        // GET: api/accounts
        [HttpGet]
        public async Task<ActionResult<IEnumerable<AccountResponse>>> GetAccounts()
        {
            var userId = GetUserId();

            var accounts = await _context.Accounts
                .Where(a => a.UserId == userId)
                .Select(a => new AccountResponse
                {
                    Id = a.Id,
                    Name = a.Name,
                    Balance = a.Balance,
                    CreatedAt = a.CreatedAt
                })
                .ToListAsync();

            return Ok(accounts);
        }

        // GET: api/accounts/5
        [HttpGet("{id}")]
        public async Task<ActionResult<AccountResponse>> GetAccount(int id)
        {
            var userId = GetUserId();

            var account = await _context.Accounts
                .Where(a => a.Id == id && a.UserId == userId)
                .Select(a => new AccountResponse
                {
                    Id = a.Id,
                    Name = a.Name,
                    Balance = a.Balance,
                    CreatedAt = a.CreatedAt
                })
                .FirstOrDefaultAsync();

            if (account == null)
                return NotFound();

            return Ok(account);
        }

        // POST: api/accounts
        [HttpPost]
        public async Task<ActionResult<AccountResponse>> CreateAccount(AccountRequest request)
        {
            var userId = GetUserId();

            var account = new Account
            {
                Name = request.Name,
                Balance = request.Balance,
                UserId = userId
            };

            _context.Accounts.Add(account);
            await _context.SaveChangesAsync();

            var response = new AccountResponse
            {
                Id = account.Id,
                Name = account.Name,
                Balance = account.Balance,
                CreatedAt = account.CreatedAt
            };

            return CreatedAtAction(nameof(GetAccount), new { id = account.Id }, response);
        }

        // PUT: api/accounts/5
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateAccount(int id, AccountRequest request)
        {
            var userId = GetUserId();

            var account = await _context.Accounts
                .FirstOrDefaultAsync(a => a.Id == id && a.UserId == userId);

            if (account == null)
                return NotFound();

            account.Name = request.Name;
            account.Balance = request.Balance;

            await _context.SaveChangesAsync();

            return NoContent();
        }

        // DELETE: api/accounts/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteAccount(int id)
        {
            var userId = GetUserId();

            var account = await _context.Accounts
                .FirstOrDefaultAsync(a => a.Id == id && a.UserId == userId);

            if (account == null)
                return NotFound();

            _context.Accounts.Remove(account);
            await _context.SaveChangesAsync();

            return NoContent();
        }
    }
}