using Microsoft.AspNetCore.Mvc;
using PersonalFinanceTracker.Api.DTOs;
using PersonalFinanceTracker.Api.Services;

namespace PersonalFinanceTracker.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly AuthService _authService;

        public AuthController(AuthService authService)
        {
            _authService = authService;
        }

        [HttpPost("register")]
        public async Task<ActionResult<AuthResponse>> Register(RegisterRequest request)
        {
            var result = await _authService.RegisterAsync(request);

            if (result == null)
                return Conflict(new { message = "An account with this email already exists." });

            return Ok(result);
        }

        [HttpPost("login")]
        public async Task<ActionResult<AuthResponse>> Login(LoginRequest request)
        {
            var result = await _authService.LoginAsync(request);

            if (result == null)
                return Unauthorized(new { message = "Invalid email or password." });

            return Ok(result);
        }
    }
}