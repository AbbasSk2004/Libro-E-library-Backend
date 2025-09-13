using System.Data;
using Dapper;
using Npgsql;

namespace E_Library.API.Data
{
    public class DatabaseConnection
    {
        private readonly string _connectionString;

        public DatabaseConnection(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection") 
                ?? throw new InvalidOperationException("Database connection string not found");
        }

        public async Task<IDbConnection> GetConnectionAsync()
        {
            try
            {
                Console.WriteLine("DatabaseConnection: Creating new connection");
                var connection = new NpgsqlConnection(_connectionString);
                
                Console.WriteLine("DatabaseConnection: Opening connection");
                await connection.OpenAsync();
                
                Console.WriteLine("DatabaseConnection: Connection opened successfully");
                return connection;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"DatabaseConnection GetConnectionAsync error: {ex.Message}");
                Console.WriteLine($"DatabaseConnection Stack trace: {ex.StackTrace}");
                throw;
            }
        }

        public async Task InitializeDatabaseAsync()
        {
            try
            {
                using var connection = await GetConnectionAsync();
                Console.WriteLine("Database connection established successfully");
                
                // Check if the Users table exists
                var tableExists = await connection.QueryFirstOrDefaultAsync<int>(
                    "SELECT COUNT(*) FROM information_schema.tables WHERE table_name = 'Users'");
                
                Console.WriteLine($"Users table exists: {tableExists > 0}");
                
                if (tableExists == 0)
                {
                    Console.WriteLine("Creating database schema...");
                    await CreateSchemaAsync(connection);
                }
                
                // Test a simple query
                var result = await connection.QueryFirstOrDefaultAsync<int>("SELECT 1");
                Console.WriteLine($"Test query result: {result}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Database initialization error: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
                throw;
            }
        }

        private async Task CreateSchemaAsync(IDbConnection connection)
        {
            var schema = @"
                CREATE TABLE IF NOT EXISTS ""Users"" (
                    ""Id"" SERIAL PRIMARY KEY,
                    ""Email"" VARCHAR(255) UNIQUE NOT NULL,
                    ""Name"" VARCHAR(255) NOT NULL,
                    ""PasswordHash"" TEXT NOT NULL,
                    ""CreatedAt"" TIMESTAMP WITH TIME ZONE DEFAULT NOW(),
                    ""UpdatedAt"" TIMESTAMP WITH TIME ZONE DEFAULT NOW(),
                    ""Role"" VARCHAR(50) NOT NULL DEFAULT 'User'
                );

                CREATE TABLE IF NOT EXISTS ""Books"" (
                    ""Id"" SERIAL PRIMARY KEY,
                    ""Title"" VARCHAR(500) NOT NULL,
                    ""Author"" VARCHAR(255) NOT NULL,
                    ""Isbn"" VARCHAR(20) UNIQUE NOT NULL,
                    ""Description"" TEXT,
                    ""PublishedYear"" INTEGER NOT NULL,
                    ""Available"" BOOLEAN DEFAULT TRUE,
                    ""CoverImage"" TEXT,
                    ""CreatedAt"" TIMESTAMP WITH TIME ZONE DEFAULT NOW(),
                    ""UpdatedAt"" TIMESTAMP WITH TIME ZONE DEFAULT NOW()
                );

                CREATE TABLE IF NOT EXISTS ""BorrowedBooks"" (
                    ""Id"" SERIAL PRIMARY KEY,
                    ""UserId"" INTEGER NOT NULL,
                    ""BookId"" INTEGER NOT NULL,
                    ""BorrowedAt"" TIMESTAMP WITH TIME ZONE DEFAULT NOW(),
                    ""DueDate"" TIMESTAMP WITH TIME ZONE NOT NULL,
                    ""Price"" DECIMAL(10,2) NOT NULL DEFAULT 0.00,
                    ""IdCardImagePath"" TEXT,
                    ""CreatedAt"" TIMESTAMP WITH TIME ZONE DEFAULT NOW(),
                    ""UpdatedAt"" TIMESTAMP WITH TIME ZONE DEFAULT NOW(),
                    FOREIGN KEY (""UserId"") REFERENCES ""Users""(""Id"") ON DELETE CASCADE,
                    FOREIGN KEY (""BookId"") REFERENCES ""Books""(""Id"") ON DELETE CASCADE
                );

                CREATE TABLE IF NOT EXISTS ""ReturnedBooks"" (
                    ""Id"" SERIAL PRIMARY KEY,
                    ""UserId"" INTEGER NOT NULL,
                    ""BookId"" INTEGER NOT NULL,
                    ""BorrowedAt"" TIMESTAMP WITH TIME ZONE NOT NULL,
                    ""DueDate"" TIMESTAMP WITH TIME ZONE NOT NULL,
                    ""ReturnedAt"" TIMESTAMP WITH TIME ZONE NOT NULL,
                    ""Price"" DECIMAL(10,2) NOT NULL DEFAULT 0.00,
                    ""IdCardImagePath"" TEXT,
                    ""CreatedAt"" TIMESTAMP WITH TIME ZONE DEFAULT NOW(),
                    ""UpdatedAt"" TIMESTAMP WITH TIME ZONE DEFAULT NOW(),
                    FOREIGN KEY (""UserId"") REFERENCES ""Users""(""Id"") ON DELETE CASCADE,
                    FOREIGN KEY (""BookId"") REFERENCES ""Books""(""Id"") ON DELETE CASCADE
                );
            ";

            await connection.ExecuteAsync(schema);
            Console.WriteLine("Database schema created successfully");
        }
    }
}
