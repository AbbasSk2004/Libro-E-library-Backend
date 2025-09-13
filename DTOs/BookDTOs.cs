namespace E_Library.API.DTOs
{
    public class BookDto
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Author { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public int PublishedYear { get; set; }
        public string Category { get; set; } = "General";
        public int NumberOfCopies { get; set; } = 1;
        public bool Available { get; set; }
        public string? CoverImage { get; set; }
    }

    public class BorrowedBookDto
    {
        public int Id { get; set; }
        public BookDto Book { get; set; } = new();
        public DateTime BorrowedAt { get; set; }
        public DateTime DueDate { get; set; }
    }

    public class BorrowBookRequest
    {
        public int BookId { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public IFormFile? IdCardImage { get; set; }
    }

    public class AdminBorrowedBookDto
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public string UserName { get; set; } = string.Empty;
        public string UserEmail { get; set; } = string.Empty;
        public int BookId { get; set; }
        public string BookTitle { get; set; } = string.Empty;
        public string BookAuthor { get; set; } = string.Empty;
        public DateTime BorrowedAt { get; set; }
        public DateTime DueDate { get; set; }
        public decimal Price { get; set; }
        public string? IdCardImagePath { get; set; }
        public string CreatedAt { get; set; } = string.Empty;
    }
}
