using Supabase;
using Supabase.Gotrue;

namespace E_Library.API.Services
{
    public interface IStorageService
    {
        Task<string> UploadIdCardAsync(IFormFile file, int userId, int bookId);
        Task<string> GetIdCardUrlAsync(string fileName);
        Task<string> UploadBookCoverAsync(IFormFile file, int bookId);
        Task<string> GetBookCoverUrlAsync(string fileName);
        Task<bool> DeleteBookCoverAsync(string fileName);
    }

    public class StorageService : IStorageService
    {
        private readonly Supabase.Client _supabase;
        private readonly string _idCardBucketName = "id-cards";
        private readonly string _bookCoverBucketName = "book-covers";

        public StorageService(IConfiguration configuration)
        {
            var supabaseUrl = Environment.GetEnvironmentVariable("SUPABASE_URL");
            var supabaseKey = Environment.GetEnvironmentVariable("SUPABASE_SERVICE_ROLE_KEY");
            
            if (string.IsNullOrEmpty(supabaseUrl) || string.IsNullOrEmpty(supabaseKey))
            {
                throw new InvalidOperationException("Supabase URL and Service Role Key must be configured");
            }

            _supabase = new Supabase.Client(supabaseUrl, supabaseKey);
        }

        public async Task<string> UploadIdCardAsync(IFormFile file, int userId, int bookId)
        {
            try
            {
                var fileName = $"{userId}/{bookId}_{Guid.NewGuid()}{Path.GetExtension(file.FileName)}";
                
                using var stream = file.OpenReadStream();
                var bytes = new byte[stream.Length];
                var totalBytesRead = 0;
                while (totalBytesRead < bytes.Length)
                {
                    var bytesRead = await stream.ReadAsync(bytes, totalBytesRead, bytes.Length - totalBytesRead);
                    if (bytesRead == 0)
                        break;
                    totalBytesRead += bytesRead;
                }
                
                var result = await _supabase.Storage
                    .From(_idCardBucketName)
                    .Upload(bytes, fileName, new Supabase.Storage.FileOptions
                    {
                        CacheControl = "3600",
                        Upsert = false
                    });

                return result;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error uploading to Supabase storage: {ex.Message}");
                throw;
            }
        }

        public Task<string> GetIdCardUrlAsync(string fileName)
        {
            try
            {
                var supabaseUrl = Environment.GetEnvironmentVariable("SUPABASE_URL");
                
                // Check if fileName already includes the bucket name
                string filePath;
                if (fileName.StartsWith($"{_idCardBucketName}/"))
                {
                    // File name already includes bucket name, use as is
                    filePath = fileName;
                }
                else
                {
                    // File name doesn't include bucket name, add it
                    filePath = $"{_idCardBucketName}/{fileName}";
                }
                
                var publicUrl = $"{supabaseUrl}/storage/v1/object/public/{filePath}";
                
                return Task.FromResult(publicUrl);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error getting Supabase storage URL: {ex.Message}");
                throw;
            }
        }

        public async Task<string> UploadBookCoverAsync(IFormFile file, int bookId)
        {
            try
            {
                var fileName = $"{bookId}_{Guid.NewGuid()}{Path.GetExtension(file.FileName)}";
                
                using var stream = file.OpenReadStream();
                var bytes = new byte[stream.Length];
                var totalBytesRead = 0;
                while (totalBytesRead < bytes.Length)
                {
                    var bytesRead = await stream.ReadAsync(bytes, totalBytesRead, bytes.Length - totalBytesRead);
                    if (bytesRead == 0)
                        break;
                    totalBytesRead += bytesRead;
                }
                
                var result = await _supabase.Storage
                    .From(_bookCoverBucketName)
                    .Upload(bytes, fileName, new Supabase.Storage.FileOptions
                    {
                        CacheControl = "3600",
                        Upsert = false
                    });

                return result;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error uploading book cover to Supabase storage: {ex.Message}");
                throw;
            }
        }

        public Task<string> GetBookCoverUrlAsync(string fileName)
        {
            try
            {
                var supabaseUrl = Environment.GetEnvironmentVariable("SUPABASE_URL");
                
                // Check if fileName already includes the bucket name
                string filePath;
                if (fileName.StartsWith($"{_bookCoverBucketName}/"))
                {
                    // File name already includes bucket name, use as is
                    filePath = fileName;
                }
                else
                {
                    // File name doesn't include bucket name, add it
                    filePath = $"{_bookCoverBucketName}/{fileName}";
                }
                
                var publicUrl = $"{supabaseUrl}/storage/v1/object/public/{filePath}";
                
                return Task.FromResult(publicUrl);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error getting Supabase storage URL: {ex.Message}");
                throw;
            }
        }

        public async Task<bool> DeleteBookCoverAsync(string fileName)
        {
            try
            {
                // Extract just the filename without bucket prefix if present
                string filePath;
                if (fileName.StartsWith($"{_bookCoverBucketName}/"))
                {
                    // Remove bucket prefix to get just the filename
                    filePath = fileName.Substring($"{_bookCoverBucketName}/".Length);
                }
                else
                {
                    filePath = fileName;
                }

                await _supabase.Storage
                    .From(_bookCoverBucketName)
                    .Remove(filePath);

                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error deleting book cover from Supabase storage: {ex.Message}");
                return false;
            }
        }
    }
}
