namespace E_Library.API.Models
{
    public class Book
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
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }
}
