using System.Data;
using Dapper;
using E_Library.API.Models;

namespace E_Library.API.Data
{
    public interface IEmailVerificationRepository
    {
        Task<EmailVerification?> GetByTokenAsync(string token);
        Task<EmailVerification?> GetByUserIdAsync(int userId);
        Task<EmailVerification> CreateAsync(EmailVerification emailVerification);
        Task<bool> MarkAsUsedAsync(string token);
        Task<bool> DeleteExpiredTokensAsync();
    }

    public class EmailVerificationRepository : IEmailVerificationRepository
    {
        private readonly DatabaseConnection _dbConnection;

        public EmailVerificationRepository(DatabaseConnection dbConnection)
        {
            _dbConnection = dbConnection;
        }

        public async Task<EmailVerification?> GetByTokenAsync(string token)
        {
            try
            {
                using var connection = await _dbConnection.GetConnectionAsync();
                var sql = @"
                    SELECT ""Id"", ""UserId"", ""Token"", ""ExpiresAt"", ""CreatedAt"", ""IsUsed"" 
                    FROM ""EmailVerifications"" 
                    WHERE ""Token"" = @Token";
                
                var parameters = new { Token = token };
                return await connection.QueryFirstOrDefaultAsync<EmailVerification>(sql, parameters);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"EmailVerificationRepository GetByTokenAsync error: {ex.Message}");
                throw;
            }
        }

        public async Task<EmailVerification?> GetByUserIdAsync(int userId)
        {
            try
            {
                using var connection = await _dbConnection.GetConnectionAsync();
                var sql = @"
                    SELECT ""Id"", ""UserId"", ""Token"", ""ExpiresAt"", ""CreatedAt"", ""IsUsed"" 
                    FROM ""EmailVerifications"" 
                    WHERE ""UserId"" = @UserId 
                    ORDER BY ""CreatedAt"" DESC 
                    LIMIT 1";
                
                var parameters = new { UserId = userId };
                return await connection.QueryFirstOrDefaultAsync<EmailVerification>(sql, parameters);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"EmailVerificationRepository GetByUserIdAsync error: {ex.Message}");
                throw;
            }
        }

        public async Task<EmailVerification> CreateAsync(EmailVerification emailVerification)
        {
            try
            {
                using var connection = await _dbConnection.GetConnectionAsync();
                var sql = @"
                    INSERT INTO ""EmailVerifications"" (""UserId"", ""Token"", ""ExpiresAt"", ""CreatedAt"", ""IsUsed"")
                    VALUES (@UserId, @Token, @ExpiresAt, @CreatedAt, @IsUsed)
                    RETURNING ""Id""";
                
                var id = await connection.QuerySingleAsync<int>(sql, emailVerification);
                emailVerification.Id = id;
                
                return emailVerification;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"EmailVerificationRepository CreateAsync error: {ex.Message}");
                throw;
            }
        }

        public async Task<bool> MarkAsUsedAsync(string token)
        {
            try
            {
                using var connection = await _dbConnection.GetConnectionAsync();
                var sql = @"
                    UPDATE ""EmailVerifications"" 
                    SET ""IsUsed"" = true 
                    WHERE ""Token"" = @Token";
                
                var parameters = new { Token = token };
                var rowsAffected = await connection.ExecuteAsync(sql, parameters);
                return rowsAffected > 0;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"EmailVerificationRepository MarkAsUsedAsync error: {ex.Message}");
                throw;
            }
        }

        public async Task<bool> DeleteExpiredTokensAsync()
        {
            try
            {
                using var connection = await _dbConnection.GetConnectionAsync();
                var sql = @"
                    DELETE FROM ""EmailVerifications"" 
                    WHERE ""ExpiresAt"" < @Now";
                
                var parameters = new { Now = DateTime.UtcNow };
                var rowsAffected = await connection.ExecuteAsync(sql, parameters);
                return rowsAffected > 0;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"EmailVerificationRepository DeleteExpiredTokensAsync error: {ex.Message}");
                throw;
            }
        }
    }
}
