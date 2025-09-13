using E_Library.API.Data;
using E_Library.API.DTOs;
using E_Library.API.Models;

namespace E_Library.API.Services
{
    public interface IBookService
    {
        Task<IEnumerable<BookDto>> GetBooksAsync(string? search = null);
        Task<BookDto?> GetBookByIdAsync(int id);
        Task<BorrowedBookDto> BorrowBookAsync(int bookId, int userId, DateTime startDate, DateTime endDate, string? idCardImagePath);
        Task ReturnBookAsync(int bookId, int userId);
        Task<IEnumerable<BorrowedBookDto>> GetBorrowedBooksAsync(int userId);
        Task<IEnumerable<AdminBorrowedBookDto>> GetAllBorrowedBooksAsync();
        Task AdminReturnBookAsync(int borrowedBookId);
    }

    public class BookService : IBookService
    {
        private readonly IBookRepository _bookRepository;
        private readonly IBorrowedBookRepository _borrowedBookRepository;
        private readonly IReturnedBookRepository _returnedBookRepository;
        private readonly IUserRepository _userRepository;
        private readonly IStorageService _storageService;

        public BookService(
            IBookRepository bookRepository, 
            IBorrowedBookRepository borrowedBookRepository,
            IReturnedBookRepository returnedBookRepository,
            IUserRepository userRepository,
            IStorageService storageService)
        {
            _bookRepository = bookRepository;
            _borrowedBookRepository = borrowedBookRepository;
            _returnedBookRepository = returnedBookRepository;
            _userRepository = userRepository;
            _storageService = storageService;
        }

        public async Task<IEnumerable<BookDto>> GetBooksAsync(string? search = null)
        {
            var books = await _bookRepository.GetAllAsync(search);
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
            
            return bookDtos;
        }

        public async Task<BookDto?> GetBookByIdAsync(int id)
        {
            var book = await _bookRepository.GetByIdAsync(id);
            if (book == null) return null;
            
            string? coverImageUrl = null;
            if (!string.IsNullOrEmpty(book.CoverImage))
            {
                coverImageUrl = await _storageService.GetBookCoverUrlAsync(book.CoverImage);
            }
            
            return new BookDto
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
        }

        public async Task<BorrowedBookDto> BorrowBookAsync(int bookId, int userId, DateTime startDate, DateTime endDate, string? idCardImagePath)
        {
            Console.WriteLine($"BookService.BorrowBookAsync: Starting borrow process for book {bookId}, user {userId}");
            
            // Check if book exists and is available
            var book = await _bookRepository.GetByIdAsync(bookId);
            if (book == null)
            {
                Console.WriteLine($"BookService.BorrowBookAsync: Book {bookId} not found");
                throw new ArgumentException("Book not found");
            }

            Console.WriteLine($"BookService.BorrowBookAsync: Book found: {book.Title}, Available: {book.Available}");

            if (!book.Available || book.NumberOfCopies <= 0)
            {
                Console.WriteLine($"BookService.BorrowBookAsync: Book {bookId} is not available (Available: {book.Available}, Copies: {book.NumberOfCopies})");
                throw new InvalidOperationException("Book is not available");
            }

            // Check if user already borrowed this book
            var isAlreadyBorrowed = await _borrowedBookRepository.IsBookBorrowedAsync(bookId, userId);
            Console.WriteLine($"BookService.BorrowBookAsync: Is book already borrowed by user: {isAlreadyBorrowed}");
            
            if (isAlreadyBorrowed)
                throw new InvalidOperationException("You have already borrowed this book");

            // Calculate price ($2 per day)
            var days = (int)(endDate - startDate).TotalDays;
            var price = days * 2;
            Console.WriteLine($"BookService.BorrowBookAsync: Calculated price: ${price} for {days} days");

            // Create borrowed book record
            var borrowedBook = new BorrowedBook
            {
                UserId = userId,
                BookId = bookId,
                BorrowedAt = startDate,
                DueDate = endDate,
                Price = price,
                IdCardImagePath = idCardImagePath,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            Console.WriteLine($"BookService.BorrowBookAsync: Creating borrowed book record...");
            var createdBorrow = await _borrowedBookRepository.CreateAsync(borrowedBook);
            Console.WriteLine($"BookService.BorrowBookAsync: Borrowed book record created with ID: {createdBorrow.Id}");

            // Decrement number of copies
            Console.WriteLine($"BookService.BorrowBookAsync: Decrementing number of copies...");
            await _bookRepository.DecrementCopiesAsync(bookId);
            Console.WriteLine($"BookService.BorrowBookAsync: Number of copies decremented");

            // Get the book with user and book details
            Console.WriteLine($"BookService.BorrowBookAsync: Fetching complete borrowed book details...");
            var result = await _borrowedBookRepository.GetByIdAsync(createdBorrow.Id);
            Console.WriteLine($"BookService.BorrowBookAsync: Retrieved borrowed book details: {result != null}");
            
            return await MapToBorrowedBookDto(result!);
        }

        public async Task ReturnBookAsync(int bookId, int userId)
        {
            Console.WriteLine($"BookService.ReturnBookAsync: Attempting to return book {bookId} for user {userId}");
            
            var activeBorrow = await _borrowedBookRepository.GetActiveBorrowAsync(bookId, userId);
            if (activeBorrow == null)
            {
                Console.WriteLine($"BookService.ReturnBookAsync: No active borrow found for book {bookId} and user {userId}");
                
                // Check if there are any borrowed books for this user/book combination
                var hasAnyBorrows = await _borrowedBookRepository.HasAnyBorrowsAsync(bookId, userId);
                if (hasAnyBorrows)
                {
                    throw new InvalidOperationException("This book has already been returned");
                }
                else
                {
                    throw new InvalidOperationException("You have not borrowed this book");
                }
            }

            Console.WriteLine($"BookService.ReturnBookAsync: Found active borrow with ID {activeBorrow.Id}");

            // Create returned book record
            var returnedBook = new ReturnedBook
            {
                UserId = activeBorrow.UserId,
                BookId = activeBorrow.BookId,
                BorrowedAt = activeBorrow.BorrowedAt,
                DueDate = activeBorrow.DueDate,
                ReturnedAt = DateTime.UtcNow,
                Price = activeBorrow.Price,
                IdCardImagePath = activeBorrow.IdCardImagePath,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            Console.WriteLine($"BookService.ReturnBookAsync: Creating returned book record...");
            await _returnedBookRepository.CreateAsync(returnedBook);
            Console.WriteLine($"BookService.ReturnBookAsync: Returned book record created");

            // Delete the borrowed book record
            Console.WriteLine($"BookService.ReturnBookAsync: Deleting borrowed book record...");
            await _borrowedBookRepository.DeleteAsync(activeBorrow.Id);
            Console.WriteLine($"BookService.ReturnBookAsync: Borrowed book record deleted");

            // Increment number of copies
            Console.WriteLine($"BookService.ReturnBookAsync: Incrementing number of copies...");
            await _bookRepository.IncrementCopiesAsync(bookId);
            Console.WriteLine($"BookService.ReturnBookAsync: Number of copies incremented");
        }

        public async Task<IEnumerable<BorrowedBookDto>> GetBorrowedBooksAsync(int userId)
        {
            var borrowedBooks = await _borrowedBookRepository.GetByUserIdAsync(userId);
            var borrowedBookDtos = new List<BorrowedBookDto>();
            
            foreach (var borrowedBook in borrowedBooks)
            {
                borrowedBookDtos.Add(await MapToBorrowedBookDto(borrowedBook));
            }
            
            return borrowedBookDtos;
        }

        private static BookDto MapToDto(Book book)
        {
            return new BookDto
            {
                Id = book.Id,
                Title = book.Title,
                Author = book.Author,
                Description = book.Description,
                PublishedYear = book.PublishedYear,
                Category = book.Category,
                NumberOfCopies = book.NumberOfCopies,
                Available = book.Available && book.NumberOfCopies > 0,
                CoverImage = book.CoverImage
            };
        }

        public async Task<IEnumerable<AdminBorrowedBookDto>> GetAllBorrowedBooksAsync()
        {
            var borrowedBooks = await _borrowedBookRepository.GetAllAsync();
            var adminBorrowedBooks = new List<AdminBorrowedBookDto>();

            foreach (var borrowedBook in borrowedBooks)
            {
                var user = await _userRepository.GetByIdAsync(borrowedBook.UserId);
                var book = await _bookRepository.GetByIdAsync(borrowedBook.BookId);

                if (user != null && book != null)
                {
                    adminBorrowedBooks.Add(new AdminBorrowedBookDto
                    {
                        Id = borrowedBook.Id,
                        UserId = borrowedBook.UserId,
                        UserName = user.Name,
                        UserEmail = user.Email,
                        BookId = borrowedBook.BookId,
                        BookTitle = book.Title,
                        BookAuthor = book.Author,
                        BorrowedAt = borrowedBook.BorrowedAt,
                        DueDate = borrowedBook.DueDate,
                        Price = borrowedBook.Price,
                        IdCardImagePath = !string.IsNullOrEmpty(borrowedBook.IdCardImagePath) 
                            ? await _storageService.GetIdCardUrlAsync(borrowedBook.IdCardImagePath)
                            : null,
                        CreatedAt = borrowedBook.CreatedAt.ToString("yyyy-MM-ddTHH:mm:ssZ")
                    });
                }
            }

            return adminBorrowedBooks.OrderByDescending(b => b.CreatedAt);
        }

        public async Task AdminReturnBookAsync(int borrowedBookId)
        {
            var borrowedBook = await _borrowedBookRepository.GetByIdAsync(borrowedBookId);
            if (borrowedBook == null)
            {
                throw new ArgumentException("Borrowed book not found");
            }

            // Move to returned books
            var returnedBook = new ReturnedBook
            {
                UserId = borrowedBook.UserId,
                BookId = borrowedBook.BookId,
                BorrowedAt = borrowedBook.BorrowedAt,
                DueDate = borrowedBook.DueDate,
                ReturnedAt = DateTime.UtcNow,
                Price = borrowedBook.Price,
                IdCardImagePath = borrowedBook.IdCardImagePath,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            await _returnedBookRepository.CreateAsync(returnedBook);

            // Increment book copies
            await _bookRepository.IncrementCopiesAsync(borrowedBook.BookId);

            // Delete from borrowed books
            await _borrowedBookRepository.DeleteAsync(borrowedBookId);
        }


        private async Task<BorrowedBookDto> MapToBorrowedBookDto(BorrowedBook borrowedBook)
        {
            string? coverImageUrl = null;
            if (!string.IsNullOrEmpty(borrowedBook.Book.CoverImage))
            {
                coverImageUrl = await _storageService.GetBookCoverUrlAsync(borrowedBook.Book.CoverImage);
            }
            
            return new BorrowedBookDto
            {
                Id = borrowedBook.Id,
                Book = new BookDto
                {
                    Id = borrowedBook.Book.Id,
                    Title = borrowedBook.Book.Title,
                    Author = borrowedBook.Book.Author,
                    Description = borrowedBook.Book.Description,
                    PublishedYear = borrowedBook.Book.PublishedYear,
                    Category = borrowedBook.Book.Category,
                    NumberOfCopies = borrowedBook.Book.NumberOfCopies,
                    Available = borrowedBook.Book.Available && borrowedBook.Book.NumberOfCopies > 0,
                    CoverImage = coverImageUrl
                },
                BorrowedAt = borrowedBook.BorrowedAt,
                DueDate = borrowedBook.DueDate
            };
        }
    }
}
