using System.Data;
using Dapper;
using E_Library.API.Models;
using E_Library.API.DTOs;

namespace E_Library.API.Data
{
    public interface IBookRepository
    {
        Task<IEnumerable<Book>> GetAllAsync(string? search = null);
        Task<Book?> GetByIdAsync(int id);
        Task<Book> CreateAsync(Book book);
        Task UpdateAsync(Book book);
        Task DeleteAsync(int id);
        Task<bool> IsAvailableAsync(int id);
        Task UpdateAvailabilityAsync(int id, bool available);
        Task DecrementCopiesAsync(int id);
        Task IncrementCopiesAsync(int id);
    }

    public class BookRepository : IBookRepository
    {
        private readonly DatabaseConnection _dbConnection;

        public BookRepository(DatabaseConnection dbConnection)
        {
            _dbConnection = dbConnection;
        }

        public async Task<IEnumerable<Book>> GetAllAsync(string? search = null)
        {
            using var connection = await _dbConnection.GetConnectionAsync();
            
            if (string.IsNullOrEmpty(search))
            {
                return await connection.QueryAsync<Book>(@"
                    SELECT ""Id"", ""Title"", ""Author"", ""Description"", ""PublishedYear"", ""Category"", ""NumberOfCopies"", ""Available"", ""CoverImage"", ""CreatedAt"", ""UpdatedAt""
                    FROM ""Books"" 
                    ORDER BY ""Title""");
            }

            // Use database-agnostic case-insensitive search
            var searchPattern = $"%{search}%";
            return await connection.QueryAsync<Book>(@"
                SELECT ""Id"", ""Title"", ""Author"", ""Description"", ""PublishedYear"", ""Category"", ""NumberOfCopies"", ""Available"", ""CoverImage"", ""CreatedAt"", ""UpdatedAt""
                FROM ""Books"" 
                WHERE LOWER(""Title"") LIKE LOWER(@Search) OR LOWER(""Author"") LIKE LOWER(@Search) OR LOWER(""Category"") LIKE LOWER(@Search)
                ORDER BY ""Title""", 
                new { Search = searchPattern });
        }

        public async Task<Book?> GetByIdAsync(int id)
        {
            using var connection = await _dbConnection.GetConnectionAsync();
            return await connection.QueryFirstOrDefaultAsync<Book>(@"
                SELECT ""Id"", ""Title"", ""Author"", ""Description"", ""PublishedYear"", ""Category"", ""NumberOfCopies"", ""Available"", ""CoverImage"", ""CreatedAt"", ""UpdatedAt""
                FROM ""Books"" 
                WHERE ""Id"" = @Id", 
                new { Id = id });
        }

        public async Task<Book> CreateAsync(Book book)
        {
            using var connection = await _dbConnection.GetConnectionAsync();
            var sql = @"
                INSERT INTO ""Books"" (""Title"", ""Author"", ""Description"", ""PublishedYear"", ""Category"", ""NumberOfCopies"", ""Available"", ""CoverImage"", ""CreatedAt"", ""UpdatedAt"")
                VALUES (@Title, @Author, @Description, @PublishedYear, @Category, @NumberOfCopies, @Available, @CoverImage, @CreatedAt, @UpdatedAt)
                RETURNING ""Id"";";
            
            var id = await connection.QuerySingleAsync<int>(sql, book);
            book.Id = id;
            return book;
        }

        public async Task UpdateAsync(Book book)
        {
            using var connection = await _dbConnection.GetConnectionAsync();
            await connection.ExecuteAsync(@"
                UPDATE ""Books"" 
                SET ""Title"" = @Title, ""Author"" = @Author, ""Description"" = @Description, 
                    ""PublishedYear"" = @PublishedYear, ""Category"" = @Category, ""NumberOfCopies"" = @NumberOfCopies, ""Available"" = @Available, ""CoverImage"" = @CoverImage, ""UpdatedAt"" = @UpdatedAt
                WHERE ""Id"" = @Id", book);
        }

        public async Task DeleteAsync(int id)
        {
            using var connection = await _dbConnection.GetConnectionAsync();
            await connection.ExecuteAsync("DELETE FROM \"Books\" WHERE \"Id\" = @Id", new { Id = id });
        }

        public async Task<bool> IsAvailableAsync(int id)
        {
            using var connection = await _dbConnection.GetConnectionAsync();
            var result = await connection.QueryFirstOrDefaultAsync<bool>(
                "SELECT \"Available\" FROM \"Books\" WHERE \"Id\" = @Id", 
                new { Id = id });
            return result;
        }

        public async Task UpdateAvailabilityAsync(int id, bool available)
        {
            using var connection = await _dbConnection.GetConnectionAsync();
            await connection.ExecuteAsync(@"
                UPDATE ""Books"" 
                SET ""Available"" = @Available, ""UpdatedAt"" = @UpdatedAt
                WHERE ""Id"" = @Id", 
                new { Id = id, Available = available, UpdatedAt = DateTime.UtcNow });
        }

        public async Task DecrementCopiesAsync(int id)
        {
            using var connection = await _dbConnection.GetConnectionAsync();
            await connection.ExecuteAsync(@"
                UPDATE ""Books"" 
                SET ""NumberOfCopies"" = ""NumberOfCopies"" - 1, ""UpdatedAt"" = @UpdatedAt
                WHERE ""Id"" = @Id AND ""NumberOfCopies"" > 0", 
                new { Id = id, UpdatedAt = DateTime.UtcNow });
        }

        public async Task IncrementCopiesAsync(int id)
        {
            using var connection = await _dbConnection.GetConnectionAsync();
            await connection.ExecuteAsync(@"
                UPDATE ""Books"" 
                SET ""NumberOfCopies"" = ""NumberOfCopies"" + 1, ""UpdatedAt"" = @UpdatedAt
                WHERE ""Id"" = @Id", 
                new { Id = id, UpdatedAt = DateTime.UtcNow });
        }
    }
}
