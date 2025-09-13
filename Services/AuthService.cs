using BCrypt.Net;
using E_Library.API.Data;
using E_Library.API.DTOs;
using E_Library.API.Models;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;

namespace E_Library.API.Services
{
    public interface IAuthService
    {
        Task<LoginResponse?> LoginAsync(LoginRequest request);
        Task<LoginResponse?> RegisterAsync(RegisterRequest request);
        Task<UserDto?> GetProfileAsync(int userId);
    }

    public class AuthService : IAuthService
    {
        private readonly IUserRepository _userRepository;
        private readonly IConfiguration _configuration;

        public AuthService(IUserRepository userRepository, IConfiguration configuration)
        {
            _userRepository = userRepository;
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

        public async Task<LoginResponse?> RegisterAsync(RegisterRequest request)
        {
            try
            {
                Console.WriteLine($"AuthService: Starting registration for email: {request.Email}");
                
                // Check if user already exists
                var existingUser = await _userRepository.GetByEmailAsync(request.Email);
                if (existingUser != null)
                {
                    Console.WriteLine($"AuthService: User already exists for email: {request.Email}");
                    return null; // User already exists
                }

                Console.WriteLine($"AuthService: User doesn't exist, creating new user");
                
                // Create new user
                var user = new User
                {
                    Email = request.Email,
                    Name = request.Name,
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
                    Role = "User", // Set default role
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                Console.WriteLine($"AuthService: User object created, calling repository");
                var createdUser = await _userRepository.CreateAsync(user);
                Console.WriteLine($"AuthService: User created with ID: {createdUser.Id}");
                
                Console.WriteLine($"AuthService: Generating JWT token");
                var token = GenerateJwtToken(createdUser);
                Console.WriteLine($"AuthService: JWT token generated successfully");

                return new LoginResponse
                {
                    Token = token,
                    User = new UserDto
                    {
                        Id = createdUser.Id,
                        Email = createdUser.Email,
                        Name = createdUser.Name,
                        Role = createdUser.Role
                    }
                };
            }
            catch (Exception ex)
            {
                Console.WriteLine($"AuthService RegisterAsync error: {ex.Message}");
                Console.WriteLine($"AuthService Stack trace: {ex.StackTrace}");
                throw;
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
                Role = user.Role
            };
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
    }
}
