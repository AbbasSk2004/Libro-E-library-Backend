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
        public async Task<ActionResult<object>> Login([FromBody] LoginRequest request)
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
                if (!result.Success)
                {
                    Console.WriteLine($"Login failed: {result.ErrorType} - {result.ErrorMessage}");
                    
                    // Return different status codes based on error type
                    if (result.ErrorType == "email_not_verified")
                    {
                        return Unauthorized(new { 
                            message = result.ErrorMessage, 
                            errorType = result.ErrorType 
                        });
                    }
                    else
                    {
                        return Unauthorized(new { 
                            message = result.ErrorMessage, 
                            errorType = result.ErrorType 
                        });
                    }
                }

                Console.WriteLine($"Login successful for user: {result.Data!.User.Email}");
                return Ok(result.Data);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Login error: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
                return StatusCode(500, new { message = "An error occurred during login", error = ex.Message, details = ex.ToString() });
            }
        }

        [HttpPost("register")]
        public async Task<ActionResult<EmailVerificationResponse>> Register([FromBody] RegisterRequest request)
        {
            try
            {
                Console.WriteLine($"Register attempt for email: {request.Email}, name: {request.Name}");
                
                if (string.IsNullOrEmpty(request.Email) || string.IsNullOrEmpty(request.Password) || string.IsNullOrEmpty(request.Name))
                {
                    Console.WriteLine("Email, password, or name is null or empty");
                    return BadRequest(new EmailVerificationResponse { Success = false, Message = "Email, password, and name are required" });
                }

                var result = await _authService.RegisterAsync(request);
                if (!result.Success)
                {
                    Console.WriteLine($"Registration failed: {result.Message}");
                    return BadRequest(result);
                }

                Console.WriteLine($"Registration successful for user: {request.Email}");
                return Ok(result);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Register error: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
                return StatusCode(500, new EmailVerificationResponse { Success = false, Message = "An error occurred during registration" });
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

        [HttpPost("verify-email")]
        public async Task<ActionResult<EmailVerificationResponse>> VerifyEmail([FromBody] EmailVerificationRequest request)
        {
            try
            {
                if (string.IsNullOrEmpty(request.Token))
                {
                    return BadRequest(new EmailVerificationResponse { Success = false, Message = "Verification code is required" });
                }

                var result = await _authService.VerifyEmailAsync(request.Token);
                return Ok(result);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Verify email error: {ex.Message}");
                return StatusCode(500, new EmailVerificationResponse { Success = false, Message = "An error occurred during email verification" });
            }
        }

        [HttpPost("resend-verification")]
        public async Task<ActionResult<EmailVerificationResponse>> ResendVerification([FromBody] ResendVerificationRequest request)
        {
            try
            {
                if (string.IsNullOrEmpty(request.Email))
                {
                    return BadRequest(new EmailVerificationResponse { Success = false, Message = "Email is required" });
                }

                var result = await _authService.ResendVerificationEmailAsync(request.Email);
                return Ok(result);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Resend verification error: {ex.Message}");
                return StatusCode(500, new EmailVerificationResponse { Success = false, Message = "An error occurred while resending verification email" });
            }
        }
    }
}
