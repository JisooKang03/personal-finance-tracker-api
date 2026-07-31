using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using PersonalFinanceTracker.Api.Data;
using PersonalFinanceTracker.Api.DTOs;
using PersonalFinanceTracker.Api.Models;
using PersonalFinanceTracker.Api.Services;

namespace PersonalFinanceTracker.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class TransactionsController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly BlobStorageService _blobStorageService;

        public TransactionsController(AppDbContext context, BlobStorageService blobStorageService)
        {
            _context = context;
            _blobStorageService = blobStorageService;
        }

        private int GetUserId()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)
                ?? User.FindFirst("sub");

            return int.Parse(userIdClaim!.Value);
        }

        private async Task<bool> UserOwnsAccount(int accountId, int userId)
        {
            return await _context.Accounts
                .AnyAsync(a => a.Id == accountId && a.UserId == userId);
        }

        // GET: api/transactions?accountId=5
        [HttpGet]
        public async Task<ActionResult<IEnumerable<TransactionResponse>>> GetTransactions([FromQuery] int? accountId)
        {
            var userId = GetUserId();

            var query = _context.Transactions
                .Include(t => t.Account)
                .Include(t => t.Category)
                .Where(t => t.Account!.UserId == userId);

            if (accountId.HasValue)
                query = query.Where(t => t.AccountId == accountId.Value);

            var transactions = await query
                .OrderByDescending(t => t.Date)
                .Select(t => new TransactionResponse
                {
                    Id = t.Id,
                    AccountId = t.AccountId,
                    AccountName = t.Account!.Name,
                    CategoryId = t.CategoryId,
                    CategoryName = t.Category!.Name,
                    Amount = t.Amount,
                    Type = t.Type,
                    Description = t.Description,
                    Date = t.Date,
                    ReceiptUrl = t.ReceiptUrl,
                    CreatedAt = t.CreatedAt
                })
                .ToListAsync();

            return Ok(transactions);
        }

        // GET: api/transactions/5
        [HttpGet("{id}")]
        public async Task<ActionResult<TransactionResponse>> GetTransaction(int id)
        {
            var userId = GetUserId();

            var transaction = await _context.Transactions
                .Include(t => t.Account)
                .Include(t => t.Category)
                .Where(t => t.Id == id && t.Account!.UserId == userId)
                .Select(t => new TransactionResponse
                {
                    Id = t.Id,
                    AccountId = t.AccountId,
                    AccountName = t.Account!.Name,
                    CategoryId = t.CategoryId,
                    CategoryName = t.Category!.Name,
                    Amount = t.Amount,
                    Type = t.Type,
                    Description = t.Description,
                    Date = t.Date,
                    ReceiptUrl = t.ReceiptUrl,
                    CreatedAt = t.CreatedAt
                })
                .FirstOrDefaultAsync();

            if (transaction == null)
                return NotFound();

            return Ok(transaction);
        }

        // POST: api/transactions
        [HttpPost]
        public async Task<ActionResult<TransactionResponse>> CreateTransaction(TransactionRequest request)
        {
            var userId = GetUserId();

            if (!await UserOwnsAccount(request.AccountId, userId))
                return Forbid();

            var categoryExists = await _context.Categories.AnyAsync(c => c.Id == request.CategoryId);
            if (!categoryExists)
                return BadRequest(new { message = "Invalid category." });

            var transaction = new Transaction
            {
                AccountId = request.AccountId,
                CategoryId = request.CategoryId,
                Amount = request.Amount,
                Type = request.Type,
                Description = request.Description,
                Date = request.Date
            };

            _context.Transactions.Add(transaction);

            var account = await _context.Accounts.FindAsync(request.AccountId);
            if (account != null)
            {
                account.Balance += request.Type == "Income" ? request.Amount : -request.Amount;
            }

            await _context.SaveChangesAsync();

            var created = await _context.Transactions
                .Include(t => t.Account)
                .Include(t => t.Category)
                .FirstAsync(t => t.Id == transaction.Id);

            var response = new TransactionResponse
            {
                Id = created.Id,
                AccountId = created.AccountId,
                AccountName = created.Account!.Name,
                CategoryId = created.CategoryId,
                CategoryName = created.Category!.Name,
                Amount = created.Amount,
                Type = created.Type,
                Description = created.Description,
                Date = created.Date,
                ReceiptUrl = created.ReceiptUrl,
                CreatedAt = created.CreatedAt
            };

            return CreatedAtAction(nameof(GetTransaction), new { id = transaction.Id }, response);
        }

        // PUT: api/transactions/5
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateTransaction(int id, TransactionRequest request)
        {
            var userId = GetUserId();

            var transaction = await _context.Transactions
                .Include(t => t.Account)
                .FirstOrDefaultAsync(t => t.Id == id && t.Account!.UserId == userId);

            if (transaction == null)
                return NotFound();

            if (!await UserOwnsAccount(request.AccountId, userId))
                return Forbid();

            var categoryExists = await _context.Categories.AnyAsync(c => c.Id == request.CategoryId);
            if (!categoryExists)
                return BadRequest(new { message = "Invalid category." });

            var oldAccount = await _context.Accounts.FindAsync(transaction.AccountId);
            if (oldAccount != null)
            {
                oldAccount.Balance -= transaction.Type == "Income" ? transaction.Amount : -transaction.Amount;
            }

            transaction.AccountId = request.AccountId;
            transaction.CategoryId = request.CategoryId;
            transaction.Amount = request.Amount;
            transaction.Type = request.Type;
            transaction.Description = request.Description;
            transaction.Date = request.Date;

            var newAccount = await _context.Accounts.FindAsync(request.AccountId);
            if (newAccount != null)
            {
                newAccount.Balance += request.Type == "Income" ? request.Amount : -request.Amount;
            }

            await _context.SaveChangesAsync();

            return NoContent();
        }

        // DELETE: api/transactions/5 (soft delete)
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteTransaction(int id)
        {
            var userId = GetUserId();

            var transaction = await _context.Transactions
                .Include(t => t.Account)
                .FirstOrDefaultAsync(t => t.Id == id && t.Account!.UserId == userId);

            if (transaction == null)
                return NotFound();

            var account = await _context.Accounts.FindAsync(transaction.AccountId);
            if (account != null)
            {
                account.Balance -= transaction.Type == "Income" ? transaction.Amount : -transaction.Amount;
            }

            transaction.IsDeleted = true;

            await _context.SaveChangesAsync();

            return NoContent();
        }

        // POST: api/transactions/5/receipt
        [HttpPost("{id}/receipt")]
        public async Task<ActionResult<TransactionResponse>> UploadReceipt(int id, IFormFile file)
        {
            var userId = GetUserId();

            var transaction = await _context.Transactions
                .Include(t => t.Account)
                .Include(t => t.Category)
                .FirstOrDefaultAsync(t => t.Id == id && t.Account!.UserId == userId);

            if (transaction == null)
                return NotFound();

            if (file == null || file.Length == 0)
                return BadRequest(new { message = "No file uploaded." });

            var allowedTypes = new[] { "image/jpeg", "image/png", "image/webp" };
            if (!allowedTypes.Contains(file.ContentType))
                return BadRequest(new { message = "Only JPEG, PNG, or WebP images are allowed." });

            if (file.Length > 5 * 1024 * 1024)
                return BadRequest(new { message = "File size must be under 5MB." });

            if (!string.IsNullOrEmpty(transaction.ReceiptUrl))
            {
                await _blobStorageService.DeleteReceiptAsync(transaction.ReceiptUrl);
            }

            var receiptUrl = await _blobStorageService.UploadReceiptAsync(file, userId, transaction.Id);

            transaction.ReceiptUrl = receiptUrl;
            await _context.SaveChangesAsync();

            var response = new TransactionResponse
            {
                Id = transaction.Id,
                AccountId = transaction.AccountId,
                AccountName = transaction.Account!.Name,
                CategoryId = transaction.CategoryId,
                CategoryName = transaction.Category!.Name,
                Amount = transaction.Amount,
                Type = transaction.Type,
                Description = transaction.Description,
                Date = transaction.Date,
                ReceiptUrl = transaction.ReceiptUrl,
                CreatedAt = transaction.CreatedAt
            };

            return Ok(response);
        }

        // GET: api/transactions/5/receipt-url
        [HttpGet("{id}/receipt-url")]
        public async Task<ActionResult> GetReceiptUrl(int id)
        {
            var userId = GetUserId();

            var transaction = await _context.Transactions
                .Include(t => t.Account)
                .FirstOrDefaultAsync(t => t.Id == id && t.Account!.UserId == userId);

            if (transaction == null)
                return NotFound();

            if (string.IsNullOrEmpty(transaction.ReceiptUrl))
                return NotFound(new { message = "This transaction has no receipt attached." });

            var sasUrl = _blobStorageService.GenerateReceiptSasUrl(transaction.ReceiptUrl);

            return Ok(new { url = sasUrl, expiresInMinutes = 15 });
        }
    }
}