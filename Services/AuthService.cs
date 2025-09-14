using BCrypt.Net;
using E_Library.API.Data;
using E_Library.API.DTOs;
using E_Library.API.Models;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using System.Security.Cryptography;

namespace E_Library.API.Services
{
    public interface IAuthService
    {
        Task<LoginResponse?> LoginAsync(LoginRequest request);
        Task<EmailVerificationResponse> RegisterAsync(RegisterRequest request);
        Task<UserDto?> GetProfileAsync(int userId);
        Task<EmailVerificationResponse> VerifyEmailAsync(string token);
        Task<EmailVerificationResponse> ResendVerificationEmailAsync(string email);
    }

    public class AuthService : IAuthService
    {
        private readonly IUserRepository _userRepository;
        private readonly IEmailVerificationRepository _emailVerificationRepository;
        private readonly IEmailService _emailService;
        private readonly IConfiguration _configuration;

        public AuthService(IUserRepository userRepository, IEmailVerificationRepository emailVerificationRepository, IEmailService emailService, IConfiguration configuration)
        {
            _userRepository = userRepository;
            _emailVerificationRepository = emailVerificationRepository;
            _emailService = emailService;
            _configuration = configuration;
        }

        public async Task<LoginResponse?> LoginAsync(LoginRequest request)
        {
            try
            {
                Console.WriteLine($"AuthService: Looking for user with email: {request.Email}");
                
                var user = await _userRepository.GetByEmailAsync(request.Email);
                if (user == null)
                {
                    Console.WriteLine($"AuthService: User not found for email: {request.Email}");
                    return null;
                }

                Console.WriteLine($"AuthService: User found: {user.Email}, checking password...");
                
                bool isPasswordValid = BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash);
                if (!isPasswordValid)
                {
                    Console.WriteLine($"AuthService: Invalid password for user: {user.Email}");
                    return null;
                }

                Console.WriteLine($"AuthService: Password valid, generating token for user: {user.Email}");
                
                var token = GenerateJwtToken(user);
                return new LoginResponse
                {
                    Token = token,
                    User = new UserDto
                    {
                        Id = user.Id,
                        Email = user.Email,
                        Name = user.Name,
                        Role = user.Role
                    }
                };
            }
            catch (Exception ex)
            {
                Console.WriteLine($"AuthService LoginAsync error: {ex.Message}");
                Console.WriteLine($"AuthService Stack trace: {ex.StackTrace}");
                throw;
            }
        }

        public async Task<EmailVerificationResponse> RegisterAsync(RegisterRequest request)
        {
            try
            {
                Console.WriteLine($"AuthService: Starting registration for email: {request.Email}");
                
                // Check if user already exists
                var existingUser = await _userRepository.GetByEmailAsync(request.Email);
                if (existingUser != null)
                {
                    Console.WriteLine($"AuthService: User already exists for email: {request.Email}");
                    return new EmailVerificationResponse
                    {
                        Success = false,
                        Message = "User with this email already exists"
                    };
                }

                Console.WriteLine($"AuthService: User doesn't exist, creating new user");
                
                // Create new user (not verified initially)
                var user = new User
                {
                    Email = request.Email,
                    Name = request.Name,
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
                    Role = "User", // Set default role
                    IsEmailVerified = false,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                Console.WriteLine($"AuthService: User object created, calling repository");
                var createdUser = await _userRepository.CreateAsync(user);
                Console.WriteLine($"AuthService: User created with ID: {createdUser.Id}");
                
                // Generate verification code
                var verificationCode = GenerateVerificationCode();
                var expiryHours = int.Parse(_configuration["AppSettings:EmailVerificationExpiryHours"] ?? "24");
                
                var emailVerification = new EmailVerification
                {
                    UserId = createdUser.Id,
                    Token = verificationCode,
                    ExpiresAt = DateTime.UtcNow.AddHours(expiryHours),
                    CreatedAt = DateTime.UtcNow,
                    IsUsed = false
                };

                await _emailVerificationRepository.CreateAsync(emailVerification);
                Console.WriteLine($"AuthService: Email verification code created");

                // Send verification email
                var emailSent = await _emailService.SendEmailVerificationAsync(createdUser.Email, createdUser.Name, verificationCode);
                
                if (emailSent)
                {
                    Console.WriteLine($"AuthService: Verification email sent successfully to {createdUser.Email}");
                    return new EmailVerificationResponse
                    {
                        Success = true,
                        Message = "Registration successful! Please check your email to verify your account."
                    };
                }
                else
                {
                    Console.WriteLine($"AuthService: Failed to send verification email to {createdUser.Email}");
                    return new EmailVerificationResponse
                    {
                        Success = false,
                        Message = "Registration successful, but failed to send verification email. Please try again later."
                    };
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"AuthService RegisterAsync error: {ex.Message}");
                Console.WriteLine($"AuthService Stack trace: {ex.StackTrace}");
                return new EmailVerificationResponse
                {
                    Success = false,
                    Message = "An error occurred during registration. Please try again."
                };
            }
        }

        public async Task<UserDto?> GetProfileAsync(int userId)
        {
            var user = await _userRepository.GetByIdAsync(userId);
            if (user == null) return null;

            return new UserDto
            {
                Id = user.Id,
                Email = user.Email,
                Name = user.Name,
                Role = user.Role,
                IsEmailVerified = user.IsEmailVerified
            };
        }

        public async Task<EmailVerificationResponse> VerifyEmailAsync(string token)
        {
            try
            {
                var emailVerification = await _emailVerificationRepository.GetByTokenAsync(token);
                if (emailVerification == null)
                {
                    return new EmailVerificationResponse
                    {
                        Success = false,
                        Message = "Invalid verification token"
                    };
                }

                if (emailVerification.IsUsed)
                {
                    return new EmailVerificationResponse
                    {
                        Success = false,
                        Message = "This verification token has already been used"
                    };
                }

                if (emailVerification.ExpiresAt < DateTime.UtcNow)
                {
                    return new EmailVerificationResponse
                    {
                        Success = false,
                        Message = "Verification token has expired. Please request a new one."
                    };
                }

                // Mark token as used
                await _emailVerificationRepository.MarkAsUsedAsync(token);

                // Update user as verified
                var user = await _userRepository.GetByIdAsync(emailVerification.UserId);
                if (user != null)
                {
                    user.IsEmailVerified = true;
                    user.UpdatedAt = DateTime.UtcNow;
                    await _userRepository.UpdateAsync(user);
                }

                return new EmailVerificationResponse
                {
                    Success = true,
                    Message = "Email verified successfully! You can now log in to your account."
                };
            }
            catch (Exception ex)
            {
                Console.WriteLine($"AuthService VerifyEmailAsync error: {ex.Message}");
                return new EmailVerificationResponse
                {
                    Success = false,
                    Message = "An error occurred during email verification. Please try again."
                };
            }
        }

        public async Task<EmailVerificationResponse> ResendVerificationEmailAsync(string email)
        {
            try
            {
                var user = await _userRepository.GetByEmailAsync(email);
                if (user == null)
                {
                    return new EmailVerificationResponse
                    {
                        Success = false,
                        Message = "User not found"
                    };
                }

                if (user.IsEmailVerified)
                {
                    return new EmailVerificationResponse
                    {
                        Success = false,
                        Message = "Email is already verified"
                    };
                }

                // Generate new verification code
                var verificationCode = GenerateVerificationCode();
                var expiryHours = int.Parse(_configuration["AppSettings:EmailVerificationExpiryHours"] ?? "24");
                
                var emailVerification = new EmailVerification
                {
                    UserId = user.Id,
                    Token = verificationCode,
                    ExpiresAt = DateTime.UtcNow.AddHours(expiryHours),
                    CreatedAt = DateTime.UtcNow,
                    IsUsed = false
                };

                await _emailVerificationRepository.CreateAsync(emailVerification);

                // Send verification email
                var emailSent = await _emailService.SendEmailVerificationAsync(user.Email, user.Name, verificationCode);
                
                if (emailSent)
                {
                    return new EmailVerificationResponse
                    {
                        Success = true,
                        Message = "Verification email sent successfully! Please check your email."
                    };
                }
                else
                {
                    return new EmailVerificationResponse
                    {
                        Success = false,
                        Message = "Failed to send verification email. Please try again later."
                    };
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"AuthService ResendVerificationEmailAsync error: {ex.Message}");
                return new EmailVerificationResponse
                {
                    Success = false,
                    Message = "An error occurred while resending verification email. Please try again."
                };
            }
        }

        private string GenerateJwtToken(User user)
        {
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(
                _configuration["Jwt:Key"] ?? "YourSuperSecretKeyThatIsAtLeast32CharactersLong!"));
            var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var claims = new[]
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Email, user.Email),
                new Claim(ClaimTypes.Name, user.Name),
                new Claim(ClaimTypes.Role, user.Role)
            };

            var token = new JwtSecurityToken(
                issuer: _configuration["Jwt:Issuer"] ?? "E-Library-API",
                audience: _configuration["Jwt:Audience"] ?? "E-Library-Client",
                claims: claims,
                expires: DateTime.UtcNow.AddDays(7),
                signingCredentials: credentials
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        private string GenerateVerificationCode()
        {
            var random = new Random();
            return random.Next(100000, 999999).ToString();
        }
    }
}
