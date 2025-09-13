using System.Data;
using Dapper;
using E_Library.API.Models;
using E_Library.API.DTOs;
using Npgsql;

namespace E_Library.API.Data
{
    public interface IUserRepository
    {
        Task<User?> GetByEmailAsync(string email);
        Task<User?> GetByIdAsync(int id);
        Task<User> CreateAsync(User user);
        Task UpdateAsync(User user);
        Task<IEnumerable<User>> GetAllAsync();
        Task DeleteAsync(int id);
    }

    public class UserRepository : IUserRepository
    {
        private readonly DatabaseConnection _dbConnection;

        public UserRepository(DatabaseConnection dbConnection)
        {
            _dbConnection = dbConnection;
        }

        public async Task<User?> GetByEmailAsync(string email)
        {
            try
            {
                Console.WriteLine($"UserRepository: Getting connection for email: {email}");
                using var connection = await _dbConnection.GetConnectionAsync();
                Console.WriteLine($"UserRepository: Connection established, querying database...");
                
                // Simple query that works with Supabase
                var sql = "SELECT \"Id\", \"Email\", \"Name\", \"PasswordHash\", \"Role\", \"CreatedAt\", \"UpdatedAt\" FROM \"Users\" WHERE \"Email\" = @Email";
                var parameters = new { Email = email };
                
                Console.WriteLine($"UserRepository: Executing user query");
                var user = await connection.QueryFirstOrDefaultAsync<User>(sql, parameters);
                
                Console.WriteLine($"UserRepository: Query completed. User found: {user != null}");
                return user;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"UserRepository GetByEmailAsync error: {ex.Message}");
                Console.WriteLine($"UserRepository Stack trace: {ex.StackTrace}");
                throw;
            }
        }

        public async Task<User?> GetByIdAsync(int id)
        {
            using var connection = await _dbConnection.GetConnectionAsync();
            return await connection.QueryFirstOrDefaultAsync<User>(
                "SELECT * FROM \"Users\" WHERE \"Id\" = @Id", 
                new { Id = id });
        }

        public async Task<User> CreateAsync(User user)
        {
            try
            {
                Console.WriteLine($"UserRepository: Creating user with email: {user.Email}");
                using var connection = await _dbConnection.GetConnectionAsync();
                Console.WriteLine($"UserRepository: Connection established for user creation");
                
                // Insert the user
                var sql = @"
                    INSERT INTO ""Users"" (""Email"", ""Name"", ""PasswordHash"", ""Role"", ""CreatedAt"", ""UpdatedAt"")
                    VALUES (@Email, @Name, @PasswordHash, @Role, @CreatedAt, @UpdatedAt)";
                
                Console.WriteLine($"UserRepository: Executing insert query for user: {user.Email}");
                await connection.ExecuteAsync(sql, user);
                Console.WriteLine($"UserRepository: Insert completed, fetching user by email");
                
                // Get the user by email to get the ID
                var createdUser = await GetByEmailAsync(user.Email);
                if (createdUser != null)
                {
                    user.Id = createdUser.Id;
                    Console.WriteLine($"UserRepository: User created successfully with ID: {user.Id}");
                }
                else
                {
                    Console.WriteLine($"UserRepository: Warning - User was inserted but could not be retrieved");
                }
                return user;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"UserRepository CreateAsync error: {ex.Message}");
                Console.WriteLine($"UserRepository Stack trace: {ex.StackTrace}");
                throw;
            }
        }

        public async Task UpdateAsync(User user)
        {
            using var connection = await _dbConnection.GetConnectionAsync();
            await connection.ExecuteAsync(@"
                UPDATE ""Users"" 
                SET ""Email"" = @Email, ""Name"" = @Name, ""PasswordHash"" = @PasswordHash, ""Role"" = @Role, ""UpdatedAt"" = @UpdatedAt
                WHERE ""Id"" = @Id", user);
        }

        public async Task<IEnumerable<User>> GetAllAsync()
        {
            using var connection = await _dbConnection.GetConnectionAsync();
            return await connection.QueryAsync<User>(
                "SELECT \"Id\", \"Email\", \"Name\", \"PasswordHash\", \"Role\", \"CreatedAt\", \"UpdatedAt\" FROM \"Users\" ORDER BY \"CreatedAt\" DESC");
        }

        public async Task DeleteAsync(int id)
        {
            using var connection = await _dbConnection.GetConnectionAsync();
            await connection.ExecuteAsync("DELETE FROM \"Users\" WHERE \"Id\" = @Id", new { Id = id });
        }
    }
}
