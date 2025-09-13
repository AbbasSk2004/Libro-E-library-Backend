using System.Data;
using Dapper;
using E_Library.API.Models;
using E_Library.API.DTOs;

namespace E_Library.API.Data
{
    public interface IBorrowedBookRepository
    {
        Task<IEnumerable<BorrowedBook>> GetAllAsync();
        Task<IEnumerable<BorrowedBook>> GetByUserIdAsync(int userId);
        Task<BorrowedBook?> GetByIdAsync(int id);
        Task<BorrowedBook> CreateAsync(BorrowedBook borrowedBook);
        Task UpdateAsync(BorrowedBook borrowedBook);
        Task DeleteAsync(int id);
        Task<bool> IsBookBorrowedAsync(int bookId, int userId);
        Task<BorrowedBook?> GetActiveBorrowAsync(int bookId, int userId);
        Task<bool> HasAnyBorrowsAsync(int bookId, int userId);
    }

    public class BorrowedBookRepository : IBorrowedBookRepository
    {
        private readonly DatabaseConnection _dbConnection;

        public BorrowedBookRepository(DatabaseConnection dbConnection)
        {
            _dbConnection = dbConnection;
        }

        public async Task<IEnumerable<BorrowedBook>> GetByUserIdAsync(int userId)
        {
            using var connection = await _dbConnection.GetConnectionAsync();
            return await connection.QueryAsync<BorrowedBook, User, Book, BorrowedBook>(@"
                SELECT bb.*, u.*, b.*
                FROM ""BorrowedBooks"" bb
                INNER JOIN ""Users"" u ON bb.""UserId"" = u.""Id""
                INNER JOIN ""Books"" b ON bb.""BookId"" = b.""Id""
                WHERE bb.""UserId"" = @UserId
                ORDER BY bb.""BorrowedAt"" DESC",
                (bb, u, b) => {
                    bb.User = u;
                    bb.Book = b;
                    return bb;
                },
                new { UserId = userId },
                splitOn: "Id,Id");
        }

        public async Task<BorrowedBook?> GetByIdAsync(int id)
        {
            using var connection = await _dbConnection.GetConnectionAsync();
            var result = await connection.QueryAsync<BorrowedBook, User, Book, BorrowedBook>(@"
                SELECT bb.*, u.*, b.*
                FROM ""BorrowedBooks"" bb
                INNER JOIN ""Users"" u ON bb.""UserId"" = u.""Id""
                INNER JOIN ""Books"" b ON bb.""BookId"" = b.""Id""
                WHERE bb.""Id"" = @Id",
                (bb, u, b) => {
                    bb.User = u;
                    bb.Book = b;
                    return bb;
                },
                new { Id = id },
                splitOn: "Id,Id");

            return result.FirstOrDefault();
        }

        public async Task<BorrowedBook> CreateAsync(BorrowedBook borrowedBook)
        {
            Console.WriteLine($"BorrowedBookRepository.CreateAsync: Creating borrowed book record for UserId={borrowedBook.UserId}, BookId={borrowedBook.BookId}");
            
            using var connection = await _dbConnection.GetConnectionAsync();
            var sql = @"
                INSERT INTO ""BorrowedBooks"" (""UserId"", ""BookId"", ""BorrowedAt"", ""DueDate"", ""Price"", ""IdCardImagePath"", ""CreatedAt"", ""UpdatedAt"")
                VALUES (@UserId, @BookId, @BorrowedAt, @DueDate, @Price, @IdCardImagePath, @CreatedAt, @UpdatedAt)
                RETURNING ""Id"";";
            
            Console.WriteLine($"BorrowedBookRepository.CreateAsync: Executing SQL insert...");
            var id = await connection.QuerySingleAsync<int>(sql, borrowedBook);
            Console.WriteLine($"BorrowedBookRepository.CreateAsync: Insert successful, got ID: {id}");
            
            borrowedBook.Id = id;
            return borrowedBook;
        }

        public async Task UpdateAsync(BorrowedBook borrowedBook)
        {
            using var connection = await _dbConnection.GetConnectionAsync();
            await connection.ExecuteAsync(@"
                UPDATE ""BorrowedBooks"" 
                SET ""UserId"" = @UserId, ""BookId"" = @BookId, ""BorrowedAt"" = @BorrowedAt, 
                    ""DueDate"" = @DueDate, ""Price"" = @Price, 
                    ""IdCardImagePath"" = @IdCardImagePath, ""UpdatedAt"" = @UpdatedAt
                WHERE ""Id"" = @Id", borrowedBook);
        }

        public async Task DeleteAsync(int id)
        {
            Console.WriteLine($"BorrowedBookRepository.DeleteAsync: Deleting borrowed book with ID {id}");
            
            using var connection = await _dbConnection.GetConnectionAsync();
            await connection.ExecuteAsync("DELETE FROM \"BorrowedBooks\" WHERE \"Id\" = @Id", new { Id = id });
            
            Console.WriteLine($"BorrowedBookRepository.DeleteAsync: Deleted borrowed book with ID {id}");
        }

        public async Task<bool> IsBookBorrowedAsync(int bookId, int userId)
        {
            using var connection = await _dbConnection.GetConnectionAsync();
            var result = await connection.QueryFirstOrDefaultAsync<int?>(@"
                SELECT ""Id"" FROM ""BorrowedBooks"" 
                WHERE ""BookId"" = @BookId AND ""UserId"" = @UserId",
                new { BookId = bookId, UserId = userId });
            
            return result.HasValue;
        }

        public async Task<BorrowedBook?> GetActiveBorrowAsync(int bookId, int userId)
        {
            using var connection = await _dbConnection.GetConnectionAsync();
            
            Console.WriteLine($"GetActiveBorrowAsync: Looking for bookId={bookId}, userId={userId}");
            
            var result = await connection.QueryAsync<BorrowedBook, User, Book, BorrowedBook>(@"
                SELECT bb.*, u.*, b.*
                FROM ""BorrowedBooks"" bb
                INNER JOIN ""Users"" u ON bb.""UserId"" = u.""Id""
                INNER JOIN ""Books"" b ON bb.""BookId"" = b.""Id""
                WHERE bb.""BookId"" = @BookId AND bb.""UserId"" = @UserId",
                (bb, u, b) => {
                    bb.User = u;
                    bb.Book = b;
                    return bb;
                },
                new { BookId = bookId, UserId = userId },
                splitOn: "Id,Id");

            var activeBorrow = result.FirstOrDefault();
            Console.WriteLine($"GetActiveBorrowAsync: Found active borrow: {activeBorrow != null}");
            if (activeBorrow != null)
            {
                Console.WriteLine($"GetActiveBorrowAsync: Borrow ID: {activeBorrow.Id}, BorrowedAt: {activeBorrow.BorrowedAt}");
            }
            
            return activeBorrow;
        }

        public async Task<bool> HasAnyBorrowsAsync(int bookId, int userId)
        {
            using var connection = await _dbConnection.GetConnectionAsync();
            var result = await connection.QueryFirstOrDefaultAsync<int?>(@"
                SELECT ""Id"" FROM ""BorrowedBooks"" 
                WHERE ""BookId"" = @BookId AND ""UserId"" = @UserId",
                new { BookId = bookId, UserId = userId });
            
            return result.HasValue;
        }

        public async Task<IEnumerable<BorrowedBook>> GetAllAsync()
        {
            using var connection = await _dbConnection.GetConnectionAsync();
            return await connection.QueryAsync<BorrowedBook>(@"
                SELECT ""Id"", ""UserId"", ""BookId"", ""BorrowedAt"", ""DueDate"", ""Price"", ""IdCardImagePath"", ""CreatedAt"", ""UpdatedAt""
                FROM ""BorrowedBooks""
                ORDER BY ""CreatedAt"" DESC");
        }
    }
}
