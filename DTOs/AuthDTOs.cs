namespace E_Library.API.DTOs
{
    public class LoginRequest
    {
        public string Email { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
    }

    public class LoginResponse
    {
        public string Token { get; set; } = string.Empty;
        public UserDto User { get; set; } = new();
    }

    public class UserDto
    {
        public int Id { get; set; }
        public string Email { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Role { get; set; } = "User";
        public bool IsEmailVerified { get; set; }
        public string? CreatedAt { get; set; }
        public string? UpdatedAt { get; set; }
    }

    public class RegisterRequest
    {
        public string Email { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
    }

    public class CreateUserRequest
    {
        public string Email { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string? Role { get; set; }
    }

    public class UpdateUserRequest
    {
        public string? Email { get; set; }
        public string? Name { get; set; }
        public string? Password { get; set; }
        public string? Role { get; set; }
    }

    public class DashboardStats
    {
        public int TotalUsers { get; set; }
        public int TotalBooks { get; set; }
        public int ActiveBorrows { get; set; }
        public int PendingReturns { get; set; }
    }

    public class CreateBookRequest
    {
        public string Title { get; set; } = string.Empty;
        public string Author { get; set; } = string.Empty;
        public string? Description { get; set; }
        public int PublishedYear { get; set; }
        public string? Category { get; set; }
        public int NumberOfCopies { get; set; } = 1;
        public bool Available { get; set; } = true;
        public string? CoverImage { get; set; }
    }

    public class UpdateBookRequest
    {
        public string? Title { get; set; }
        public string? Author { get; set; }
        public string? Description { get; set; }
        public int? PublishedYear { get; set; }
        public string? Category { get; set; }
        public int? NumberOfCopies { get; set; }
        public bool? Available { get; set; }
        public string? CoverImage { get; set; }
    }

    public class EmailVerificationRequest
    {
        public string Token { get; set; } = string.Empty; // This will contain the verification code
    }

    public class ResendVerificationRequest
    {
        public string Email { get; set; } = string.Empty;
    }

    public class EmailVerificationResponse
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
    }
}
