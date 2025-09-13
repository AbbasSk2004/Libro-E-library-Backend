using System.Data;
using Dapper;
using E_Library.API.Models;

namespace E_Library.API.Data
{
    public interface IReturnedBookRepository
    {
        Task<ReturnedBook> CreateAsync(ReturnedBook returnedBook);
        Task<IEnumerable<ReturnedBook>> GetByUserIdAsync(int userId);
        Task<ReturnedBook?> GetByIdAsync(int id);
    }

    public class ReturnedBookRepository : IReturnedBookRepository
    {
        private readonly DatabaseConnection _dbConnection;

        public ReturnedBookRepository(DatabaseConnection dbConnection)
        {
            _dbConnection = dbConnection;
        }

        public async Task<ReturnedBook> CreateAsync(ReturnedBook returnedBook)
        {
            Console.WriteLine($"ReturnedBookRepository.CreateAsync: Creating returned book record for UserId={returnedBook.UserId}, BookId={returnedBook.BookId}");
            
            using var connection = await _dbConnection.GetConnectionAsync();
            var sql = @"
                INSERT INTO ""ReturnedBooks"" (""UserId"", ""BookId"", ""BorrowedAt"", ""DueDate"", ""ReturnedAt"", ""Price"", ""IdCardImagePath"", ""CreatedAt"", ""UpdatedAt"")
                VALUES (@UserId, @BookId, @BorrowedAt, @DueDate, @ReturnedAt, @Price, @IdCardImagePath, @CreatedAt, @UpdatedAt)
                RETURNING ""Id"";";
            
            Console.WriteLine($"ReturnedBookRepository.CreateAsync: Executing SQL insert...");
            var id = await connection.QuerySingleAsync<int>(sql, returnedBook);
            Console.WriteLine($"ReturnedBookRepository.CreateAsync: Insert successful, got ID: {id}");
            
            returnedBook.Id = id;
            return returnedBook;
        }

        public async Task<IEnumerable<ReturnedBook>> GetByUserIdAsync(int userId)
        {
            using var connection = await _dbConnection.GetConnectionAsync();
            return await connection.QueryAsync<ReturnedBook, User, Book, ReturnedBook>(@"
                SELECT rb.*, u.*, b.*
                FROM ""ReturnedBooks"" rb
                INNER JOIN ""Users"" u ON rb.""UserId"" = u.""Id""
                INNER JOIN ""Books"" b ON rb.""BookId"" = b.""Id""
                WHERE rb.""UserId"" = @UserId
                ORDER BY rb.""ReturnedAt"" DESC",
                (rb, u, b) => {
                    rb.User = u;
                    rb.Book = b;
                    return rb;
                },
                new { UserId = userId },
                splitOn: "Id,Id");
        }

        public async Task<ReturnedBook?> GetByIdAsync(int id)
        {
            using var connection = await _dbConnection.GetConnectionAsync();
            var result = await connection.QueryAsync<ReturnedBook, User, Book, ReturnedBook>(@"
                SELECT rb.*, u.*, b.*
                FROM ""ReturnedBooks"" rb
                INNER JOIN ""Users"" u ON rb.""UserId"" = u.""Id""
                INNER JOIN ""Books"" b ON rb.""BookId"" = b.""Id""
                WHERE rb.""Id"" = @Id",
                (rb, u, b) => {
                    rb.User = u;
                    rb.Book = b;
                    return rb;
                },
                new { Id = id },
                splitOn: "Id,Id");

            return result.FirstOrDefault();
        }
    }
}
