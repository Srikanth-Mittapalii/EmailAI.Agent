using EmailAI.Agent.Services;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

namespace EmailAI.Agent.Controllers
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

        [HttpPost("signup")]
        public async Task<IActionResult> Signup([FromBody] AuthRequest request)
        {
            var user = await _authService.Signup(request.Email, request.Password);
            if (user == null)
                return BadRequest(new { message = "User with this email already exists." });

            return Ok(new { message = "User registered successfully." });
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] AuthRequest request)
        {
            var token = await _authService.Login(request.Email, request.Password);
            if (token == null)
                return Unauthorized(new { message = "Invalid email or password." });

            return Ok(new { token });
        }

        [HttpPost("forgot-password")]
        public IActionResult ForgotPassword([FromBody] ForgotPasswordRequest request)
        {
            // Mock implementation as requested
            return Ok(new { message = "If this email is registered, you will receive a password reset link shortly." });
        }
    }

    public class AuthRequest
    {
        public string Email { get; set; } = null!;
        public string Password { get; set; } = null!;
    }

    public class ForgotPasswordRequest
    {
        public string Email { get; set; } = null!;
    }
}
