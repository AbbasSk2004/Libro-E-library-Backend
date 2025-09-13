using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using E_Library.API.Data;
using E_Library.API.DTOs;
using E_Library.API.Models;
using E_Library.API.Services;
using BCrypt.Net;

namespace E_Library.API.Controllers
{
    [ApiController]
    [Route("api/admin")]
    [Authorize(Roles = "Admin")]
    public class AdminController : ControllerBase
    {
        private readonly IUserRepository _userRepository;
        private readonly IBookRepository _bookRepository;
        private readonly IBookService _bookService;
        private readonly IStorageService _storageService;

        public AdminController(IUserRepository userRepository, IBookRepository bookRepository, IBookService bookService, IStorageService storageService)
        {
            _userRepository = userRepository;
            _bookRepository = bookRepository;
            _bookService = bookService;
            _storageService = storageService;
        }

        [HttpGet("users")]
        public async Task<ActionResult<IEnumerable<UserDto>>> GetUsers()
        {
            try
            {
                var users = await _userRepository.GetAllAsync();
                var userDtos = users.Select(u => new UserDto
                {
                    Id = u.Id,
                    Email = u.Email,
                    Name = u.Name,
                    Role = u.Role,
                    CreatedAt = u.CreatedAt.ToString("yyyy-MM-ddTHH:mm:ssZ"),
                    UpdatedAt = u.UpdatedAt.ToString("yyyy-MM-ddTHH:mm:ssZ")
                });

                return Ok(userDtos);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An error occurred while fetching users", error = ex.Message });
            }
        }

        [HttpPost("users")]
        public async Task<ActionResult<UserDto>> CreateUser([FromBody] CreateUserRequest request)
        {
            try
            {
                if (string.IsNullOrEmpty(request.Email) || string.IsNullOrEmpty(request.Name) || string.IsNullOrEmpty(request.Password))
                {
                    return BadRequest(new { message = "Email, name, and password are required" });
                }

                // Check if user already exists
                var existingUser = await _userRepository.GetByEmailAsync(request.Email);
                if (existingUser != null)
                {
                    return BadRequest(new { message = "User with this email already exists" });
                }

                var user = new User
                {
                    Email = request.Email,
                    Name = request.Name,
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
                    Role = request.Role ?? "User",
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                var createdUser = await _userRepository.CreateAsync(user);

                var userDto = new UserDto
                {
                    Id = createdUser.Id,
                    Email = createdUser.Email,
                    Name = createdUser.Name,
                    Role = createdUser.Role,
                    CreatedAt = createdUser.CreatedAt.ToString("yyyy-MM-ddTHH:mm:ssZ"),
                    UpdatedAt = createdUser.UpdatedAt.ToString("yyyy-MM-ddTHH:mm:ssZ")
                };

                return Ok(userDto);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An error occurred while creating user", error = ex.Message });
            }
        }

        [HttpPut("users/{id}")]
        public async Task<ActionResult<UserDto>> UpdateUser(int id, [FromBody] UpdateUserRequest request)
        {
            try
            {
                var user = await _userRepository.GetByIdAsync(id);
                if (user == null)
                {
                    return NotFound(new { message = "User not found" });
                }

                // Update only provided fields
                if (!string.IsNullOrEmpty(request.Email))
                    user.Email = request.Email;
                if (!string.IsNullOrEmpty(request.Name))
                    user.Name = request.Name;
                if (!string.IsNullOrEmpty(request.Password))
                    user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password);
                if (!string.IsNullOrEmpty(request.Role))
                    user.Role = request.Role;

                user.UpdatedAt = DateTime.UtcNow;

                await _userRepository.UpdateAsync(user);

                var userDto = new UserDto
                {
                    Id = user.Id,
                    Email = user.Email,
                    Name = user.Name,
                    Role = user.Role,
                    CreatedAt = user.CreatedAt.ToString("yyyy-MM-ddTHH:mm:ssZ"),
                    UpdatedAt = user.UpdatedAt.ToString("yyyy-MM-ddTHH:mm:ssZ")
                };

                return Ok(userDto);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An error occurred while updating user", error = ex.Message });
            }
        }

        [HttpDelete("users/{id}")]
        public async Task<IActionResult> DeleteUser(int id)
        {
            try
            {
                var user = await _userRepository.GetByIdAsync(id);
                if (user == null)
                {
                    return NotFound(new { message = "User not found" });
                }

                await _userRepository.DeleteAsync(id);
                return Ok(new { message = "User deleted successfully" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An error occurred while deleting user", error = ex.Message });
            }
        }

        [HttpGet("dashboard/stats")]
        public async Task<ActionResult<DashboardStats>> GetDashboardStats()
        {
            try
            {
                var users = await _userRepository.GetAllAsync();
                var books = await _bookRepository.GetAllAsync();
                var borrowedBooks = await _bookService.GetAllBorrowedBooksAsync();
                
                var totalUsers = users.Count();
                var totalBooks = books.Count();
                var activeBorrows = borrowedBooks.Count();
                
                // Calculate pending returns (books due within 3 days)
                var threeDaysFromNow = DateTime.UtcNow.AddDays(3);
                var pendingReturns = borrowedBooks.Count(b => b.DueDate <= threeDaysFromNow);

                var stats = new DashboardStats
                {
                    TotalUsers = totalUsers,
                    TotalBooks = totalBooks,
                    ActiveBorrows = activeBorrows,
                    PendingReturns = pendingReturns
                };

                return Ok(stats);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An error occurred while fetching dashboard stats", error = ex.Message });
            }
        }

        [HttpGet("dashboard/recent-activity")]
        public async Task<ActionResult<IEnumerable<object>>> GetRecentActivity()
        {
            try
            {
                var activities = new List<object>();
                
                // Get recent borrowed books (last 10)
                var recentBorrows = await _bookService.GetAllBorrowedBooksAsync();
                var recentBorrowsList = recentBorrows
                    .OrderByDescending(b => b.BorrowedAt)
                    .Take(5)
                    .Select(b => new
                    {
                        id = b.Id,
                        type = "borrow",
                        user = b.UserName,
                        book = b.BookTitle,
                        time = GetTimeAgo(b.BorrowedAt),
                        status = "completed"
                    });

                activities.AddRange(recentBorrowsList);

                // Get recent users (last 5)
                var recentUsers = await _userRepository.GetAllAsync();
                var recentUsersList = recentUsers
                    .OrderByDescending(u => u.CreatedAt)
                    .Take(3)
                    .Select(u => new
                    {
                        id = u.Id,
                        type = "register",
                        user = u.Name,
                        book = (string?)null,
                        time = GetTimeAgo(u.CreatedAt),
                        status = "completed"
                    });

                activities.AddRange(recentUsersList);

                // Get recent books (last 5)
                var recentBooks = await _bookRepository.GetAllAsync();
                var recentBooksList = recentBooks
                    .OrderByDescending(b => b.CreatedAt)
                    .Take(2)
                    .Select(b => new
                    {
                        id = b.Id,
                        type = "add_book",
                        user = "Admin",
                        book = b.Title,
                        time = GetTimeAgo(b.CreatedAt),
                        status = "completed"
                    });

                activities.AddRange(recentBooksList);

                // Sort all activities by time and take the most recent 10
                var sortedActivities = activities
                    .OrderByDescending(a => ((dynamic)a).time)
                    .Take(10)
                    .ToList();

                return Ok(sortedActivities);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An error occurred while fetching recent activity", error = ex.Message });
            }
        }

        private string GetTimeAgo(DateTime dateTime)
        {
            var timeSpan = DateTime.UtcNow - dateTime;
            
            if (timeSpan.TotalMinutes < 1)
                return "Just now";
            else if (timeSpan.TotalMinutes < 60)
                return $"{(int)timeSpan.TotalMinutes} minutes ago";
            else if (timeSpan.TotalHours < 24)
                return $"{(int)timeSpan.TotalHours} hours ago";
            else
                return $"{(int)timeSpan.TotalDays} days ago";
        }

        [HttpGet("books")]
        public async Task<ActionResult<IEnumerable<BookDto>>> GetBooks()
        {
            try
            {
                var books = await _bookRepository.GetAllAsync();
                var bookDtos = new List<BookDto>();
                
                foreach (var book in books)
                {
                    string? coverImageUrl = null;
                    if (!string.IsNullOrEmpty(book.CoverImage))
                    {
                        coverImageUrl = await _storageService.GetBookCoverUrlAsync(book.CoverImage);
                    }
                    
                    bookDtos.Add(new BookDto
                    {
                        Id = book.Id,
                        Title = book.Title,
                        Author = book.Author,
                        Description = book.Description,
                        PublishedYear = book.PublishedYear,
                        Category = book.Category,
                        NumberOfCopies = book.NumberOfCopies,
                        Available = book.Available && book.NumberOfCopies > 0,
                        CoverImage = coverImageUrl
                    });
                }

                return Ok(bookDtos);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An error occurred while fetching books", error = ex.Message });
            }
        }

        [HttpPost("books")]
        public async Task<ActionResult<BookDto>> CreateBook()
        {
            try
            {
                // Parse form data
                var form = await Request.ReadFormAsync();
                var title = form["title"].FirstOrDefault();
                var author = form["author"].FirstOrDefault();
                var category = form["category"].FirstOrDefault();
                var description = form["description"].FirstOrDefault();
                var publicationYearStr = form["publishedYear"].FirstOrDefault();
                var numberOfCopiesStr = form["numberOfCopies"].FirstOrDefault();
                var availableStr = form["available"].FirstOrDefault();
                var coverImage = form.Files["coverImage"];

                if (string.IsNullOrEmpty(title) || string.IsNullOrEmpty(author))
                {
                    return BadRequest(new { message = "Title and author are required" });
                }

                if (!int.TryParse(publicationYearStr, out var publicationYear))
                {
                    return BadRequest(new { message = "Invalid publication year" });
                }

                if (!int.TryParse(numberOfCopiesStr, out var numberOfCopies))
                {
                    numberOfCopies = 1;
                }

                if (!bool.TryParse(availableStr, out var available))
                {
                    available = true;
                }

                // Handle cover image upload
                string? coverImagePath = null;
                if (coverImage != null && coverImage.Length > 0)
                {
                    // First create the book to get the ID
                    var tempBook = new Book
                    {
                        Title = title,
                        Author = author,
                        Description = description ?? string.Empty,
                        PublishedYear = publicationYear,
                        Category = category ?? "General",
                        NumberOfCopies = numberOfCopies,
                        Available = available,
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow
                    };

                    var createdBook = await _bookRepository.CreateAsync(tempBook);

                    // Upload cover image
                    coverImagePath = await _storageService.UploadBookCoverAsync(coverImage, createdBook.Id);

                    // Update book with cover image path
                    createdBook.CoverImage = coverImagePath;
                    await _bookRepository.UpdateAsync(createdBook);

                    // Get public URL for cover image
                    var coverImageUrl = await _storageService.GetBookCoverUrlAsync(coverImagePath);

                    var bookDto = new BookDto
                    {
                        Id = createdBook.Id,
                        Title = createdBook.Title,
                        Author = createdBook.Author,
                        Description = createdBook.Description,
                        PublishedYear = createdBook.PublishedYear,
                        Category = createdBook.Category,
                        NumberOfCopies = createdBook.NumberOfCopies,
                        Available = createdBook.Available && createdBook.NumberOfCopies > 0,
                        CoverImage = coverImageUrl
                    };

                    return Ok(bookDto);
                }
                else
                {
                    // Create book without cover image
                    var book = new Book
                    {
                        Title = title,
                        Author = author,
                        Description = description ?? string.Empty,
                        PublishedYear = publicationYear,
                        Category = category ?? "General",
                        NumberOfCopies = numberOfCopies,
                        Available = available,
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow
                    };

                    var createdBook = await _bookRepository.CreateAsync(book);

                    var bookDto = new BookDto
                    {
                        Id = createdBook.Id,
                        Title = createdBook.Title,
                        Author = createdBook.Author,
                        Description = createdBook.Description,
                        PublishedYear = createdBook.PublishedYear,
                        Category = createdBook.Category,
                        NumberOfCopies = createdBook.NumberOfCopies,
                        Available = createdBook.Available && createdBook.NumberOfCopies > 0,
                        CoverImage = createdBook.CoverImage
                    };

                    return Ok(bookDto);
                }
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An error occurred while creating book", error = ex.Message });
            }
        }

        [HttpPut("books/{id}")]
        public async Task<ActionResult<BookDto>> UpdateBook(int id, [FromBody] UpdateBookRequest request)
        {
            try
            {
                var book = await _bookRepository.GetByIdAsync(id);
                if (book == null)
                {
                    return NotFound(new { message = "Book not found" });
                }

                // Update only provided fields
                if (!string.IsNullOrEmpty(request.Title))
                    book.Title = request.Title;
                if (!string.IsNullOrEmpty(request.Author))
                    book.Author = request.Author;
                if (request.Description != null)
                    book.Description = request.Description;
                if (request.PublishedYear.HasValue)
                    book.PublishedYear = request.PublishedYear.Value;
                if (!string.IsNullOrEmpty(request.Category))
                    book.Category = request.Category;
                if (request.NumberOfCopies.HasValue)
                    book.NumberOfCopies = request.NumberOfCopies.Value;
                if (request.Available.HasValue)
                    book.Available = request.Available.Value;
                if (request.CoverImage != null)
                    book.CoverImage = request.CoverImage;

                book.UpdatedAt = DateTime.UtcNow;

                await _bookRepository.UpdateAsync(book);

                // Get public URL for cover image
                string? coverImageUrl = null;
                if (!string.IsNullOrEmpty(book.CoverImage))
                {
                    coverImageUrl = await _storageService.GetBookCoverUrlAsync(book.CoverImage);
                }

                var bookDto = new BookDto
                {
                    Id = book.Id,
                    Title = book.Title,
                    Author = book.Author,
                    Description = book.Description,
                    PublishedYear = book.PublishedYear,
                    Category = book.Category,
                    NumberOfCopies = book.NumberOfCopies,
                    Available = book.Available && book.NumberOfCopies > 0,
                    CoverImage = coverImageUrl
                };

                return Ok(bookDto);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An error occurred while updating book", error = ex.Message });
            }
        }

        [HttpDelete("books/{id}")]
        public async Task<IActionResult> DeleteBook(int id)
        {
            try
            {
                var book = await _bookRepository.GetByIdAsync(id);
                if (book == null)
                {
                    return NotFound(new { message = "Book not found" });
                }

                await _bookRepository.DeleteAsync(id);
                return Ok(new { message = "Book deleted successfully" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An error occurred while deleting book", error = ex.Message });
            }
        }

        [HttpGet("borrows")]
        public async Task<ActionResult<IEnumerable<AdminBorrowedBookDto>>> GetBorrows()
        {
            try
            {
                // Get all borrowed books with user and book details
                var borrowedBooks = await _bookService.GetAllBorrowedBooksAsync();
                return Ok(borrowedBooks);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An error occurred while fetching borrows", error = ex.Message });
            }
        }

        [HttpPost("borrows/{id}/return")]
        public async Task<IActionResult> AdminReturnBook(int id)
        {
            try
            {
                await _bookService.AdminReturnBookAsync(id);
                return Ok(new { message = "Book returned successfully" });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An error occurred while returning the book", error = ex.Message });
            }
        }

    }
}
