using Microsoft.AspNetCore.Mvc;
using E_Library.API.DTOs;
using E_Library.API.Services;
using Microsoft.AspNetCore.Authorization;

namespace E_Library.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class BooksController : ControllerBase
    {
        private readonly IBookService _bookService;
        private readonly IStorageService _storageService;

        public BooksController(IBookService bookService, IStorageService storageService)
        {
            _bookService = bookService;
            _storageService = storageService;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<BookDto>>> GetBooks([FromQuery] string? search = null)
        {
            try
            {
                var books = await _bookService.GetBooksAsync(search);
                return Ok(books);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An error occurred while fetching books", error = ex.Message });
            }
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<BookDto>> GetBook(int id)
        {
            try
            {
                var book = await _bookService.GetBookByIdAsync(id);
                if (book == null)
                {
                    return NotFound(new { message = "Book not found" });
                }

                return Ok(book);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An error occurred while fetching the book", error = ex.Message });
            }
        }

        [HttpPost("{id}/borrow")]
        [Authorize]
        public async Task<ActionResult<BorrowedBookDto>> BorrowBook(int id)
        {
            try
            {
                var userId = int.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? "0");
                
                // Parse form data
                var form = await Request.ReadFormAsync();
                var startDateStr = form["StartDate"].FirstOrDefault();
                var endDateStr = form["EndDate"].FirstOrDefault();
                var idCardImage = form.Files["IdCardImage"];
                
                if (string.IsNullOrEmpty(startDateStr) || string.IsNullOrEmpty(endDateStr))
                {
                    return BadRequest(new { message = "StartDate and EndDate are required" });
                }
                
                if (!DateTime.TryParse(startDateStr, out var startDate) || !DateTime.TryParse(endDateStr, out var endDate))
                {
                    return BadRequest(new { message = "Invalid date format" });
                }
                
                // Handle file upload for ID card image
                string? idCardImagePath = null;
                if (idCardImage != null && idCardImage.Length > 0)
                {
                    // Upload to Supabase storage
                    idCardImagePath = await _storageService.UploadIdCardAsync(idCardImage, userId, id);
                }

                var borrowedBook = await _bookService.BorrowBookAsync(id, userId, startDate, endDate, idCardImagePath);
                return Ok(borrowedBook);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An error occurred while borrowing the book", error = ex.Message });
            }
        }
        

        [HttpPost("{id}/return")]
        [Authorize]
        public async Task<IActionResult> ReturnBook(int id)
        {
            try
            {
                var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
                Console.WriteLine($"BooksController.ReturnBook: User ID claim value: {userIdClaim}");
                
                if (string.IsNullOrEmpty(userIdClaim))
                {
                    Console.WriteLine("BooksController.ReturnBook: No user ID found in claims");
                    return BadRequest(new { message = "User ID not found in token" });
                }
                
                var userId = int.Parse(userIdClaim);
                Console.WriteLine($"BooksController.ReturnBook: Parsed user ID: {userId}, Book ID: {id}");
                
                await _bookService.ReturnBookAsync(id, userId);
                return Ok(new { message = "Book returned successfully" });
            }
            catch (InvalidOperationException ex)
            {
                Console.WriteLine($"BooksController.ReturnBook: InvalidOperationException: {ex.Message}");
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"BooksController.ReturnBook: Exception: {ex.Message}");
                Console.WriteLine($"BooksController.ReturnBook: Stack trace: {ex.StackTrace}");
                return StatusCode(500, new { message = "An error occurred while returning the book", error = ex.Message });
            }
        }

        [HttpGet("borrowed-books")]
        [Authorize]
        public async Task<ActionResult<IEnumerable<BorrowedBookDto>>> GetBorrowedBooks()
        {
            try
            {
                var userId = int.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? "0");
                var borrowedBooks = await _bookService.GetBorrowedBooksAsync(userId);
                return Ok(borrowedBooks);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An error occurred while fetching borrowed books", error = ex.Message });
            }
        }
    }
}
