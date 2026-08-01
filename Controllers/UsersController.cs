using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using PersonalFinanceTracker.Api.Data;
using PersonalFinanceTracker.Api.Services;

namespace PersonalFinanceTracker.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class UsersController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly BlobStorageService _blobStorageService;

        public UsersController(AppDbContext context, BlobStorageService blobStorageService)
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

        // POST: api/users/me/photo
        [HttpPost("me/photo")]
        public async Task<ActionResult> UploadProfilePhoto(IFormFile file)
        {
            var userId = GetUserId();

            var user = await _context.Users.FindAsync(userId);
            if (user == null)
                return NotFound();

            if (file == null || file.Length == 0)
                return BadRequest(new { message = "No file uploaded." });

            var allowedTypes = new[] { "image/jpeg", "image/png", "image/webp" };
            if (!allowedTypes.Contains(file.ContentType))
                return BadRequest(new { message = "Only JPEG, PNG, or WebP images are allowed." });

            if (file.Length > 5 * 1024 * 1024)
                return BadRequest(new { message = "File size must be under 5MB." });

            if (!string.IsNullOrEmpty(user.ProfilePhotoUrl))
            {
                await _blobStorageService.DeleteProfilePhotoAsync(user.ProfilePhotoUrl);
            }

            var photoUrl = await _blobStorageService.UploadProfilePhotoAsync(file, userId);

            user.ProfilePhotoUrl = photoUrl;
            await _context.SaveChangesAsync();

            var sasUrl = _blobStorageService.GenerateProfilePhotoSasUrl(photoUrl);

            return Ok(new { url = sasUrl });
        }

        // GET: api/users/me/photo
        [HttpGet("me/photo")]
        public async Task<ActionResult> GetProfilePhotoUrl()
        {
            var userId = GetUserId();

            var user = await _context.Users.FindAsync(userId);
            if (user == null)
                return NotFound();

            if (string.IsNullOrEmpty(user.ProfilePhotoUrl))
                return NotFound(new { message = "No profile photo set." });

            var sasUrl = _blobStorageService.GenerateProfilePhotoSasUrl(user.ProfilePhotoUrl);

            return Ok(new { url = sasUrl });
        }

        // DELETE: api/users/me/photo
        [HttpDelete("me/photo")]
        public async Task<ActionResult> DeleteProfilePhoto()
        {
            var userId = GetUserId();

            var user = await _context.Users.FindAsync(userId);
            if (user == null)
                return NotFound();

            if (string.IsNullOrEmpty(user.ProfilePhotoUrl))
                return NotFound(new { message = "No profile photo to delete." });

            await _blobStorageService.DeleteProfilePhotoAsync(user.ProfilePhotoUrl);

            user.ProfilePhotoUrl = null;
            await _context.SaveChangesAsync();

            return NoContent();
        }
    }
}