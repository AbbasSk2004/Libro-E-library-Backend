using Microsoft.AspNetCore.Mvc;
using E_Library.API.DTOs;
using E_Library.API.Services;
using Microsoft.AspNetCore.Authorization;

namespace E_Library.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _authService;

        public AuthController(IAuthService authService)
        {
            _authService = authService;
        }

        [HttpPost("login")]
        public async Task<ActionResult<LoginResponse>> Login([FromBody] LoginRequest request)
        {
            try
            {
                Console.WriteLine($"Login attempt for email: {request.Email}");
                
                if (string.IsNullOrEmpty(request.Email) || string.IsNullOrEmpty(request.Password))
                {
                    Console.WriteLine("Email or password is null or empty");
                    return BadRequest(new { message = "Email and password are required" });
                }

                var result = await _authService.LoginAsync(request);
                if (result == null)
                {
                    Console.WriteLine("Login failed - invalid credentials");
                    return Unauthorized(new { message = "Invalid email or password" });
                }

                Console.WriteLine($"Login successful for user: {result.User.Email}");
                return Ok(result);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Login error: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
                return StatusCode(500, new { message = "An error occurred during login", error = ex.Message, details = ex.ToString() });
            }
        }

        [HttpPost("register")]
        public async Task<ActionResult<LoginResponse>> Register([FromBody] RegisterRequest request)
        {
            try
            {
                Console.WriteLine($"Register attempt for email: {request.Email}, name: {request.Name}");
                
                if (string.IsNullOrEmpty(request.Email) || string.IsNullOrEmpty(request.Password) || string.IsNullOrEmpty(request.Name))
                {
                    Console.WriteLine("Email, password, or name is null or empty");
                    return BadRequest(new { message = "Email, password, and name are required" });
                }

                var result = await _authService.RegisterAsync(request);
                if (result == null)
                {
                    Console.WriteLine("Registration failed - user already exists");
                    return BadRequest(new { message = "User with this email already exists" });
                }

                Console.WriteLine($"Registration successful for user: {result.User.Email}");
                return Ok(result);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Register error: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
                return StatusCode(500, new { message = "An error occurred during registration", error = ex.Message, details = ex.ToString() });
            }
        }

        [HttpPost("logout")]
        [Authorize]
        public IActionResult Logout()
        {
            // In a real application, you might want to blacklist the token
            // For now, we'll just return success as the client will remove the token
            return Ok(new { message = "Logged out successfully" });
        }

        [HttpGet("profile")]
        [Authorize]
        public async Task<ActionResult<UserDto>> GetProfile()
        {
            try
            {
                var userId = int.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? "0");
                var profile = await _authService.GetProfileAsync(userId);
                
                if (profile == null)
                {
                    return NotFound(new { message = "User not found" });
                }

                return Ok(profile);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An error occurred while fetching profile", error = ex.Message });
            }
        }
    }
}
