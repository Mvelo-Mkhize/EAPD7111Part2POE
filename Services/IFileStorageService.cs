using Microsoft.AspNetCore.Http;
using System.Threading.Tasks;

namespace EAPD7111Part2POE.Services
{
    public interface IFileStorageService
    {
        // Enhanced method with validation result
        Task<FileUploadResult> SaveFileAsync(IFormFile file, string subDirectory = "contracts");

        // Delete file by path
        bool DeleteFile(string filePath);

        // Get file with metadata
        Task<FileDownloadResult> GetFileAsync(string filePath);

        // Legacy method for backward compatibility
        byte[] GetFile(string filePath);

        // Validation helper
        bool ValidateFile(IFormFile file);

        // Utility method for file size display
        string GetFileSizeDisplay(long bytes);
    }

    public class FileUploadResult
    {
        public bool Success { get; set; }
        public string ErrorMessage { get; set; }
        public string FilePath { get; set; }
        public string FileName { get; set; }
        public string OriginalFileName { get; set; }
        public long FileSize { get; set; }
        public string ContentType { get; set; }
    }

    public class FileDownloadResult
    {
        public bool Success { get; set; }
        public string ErrorMessage { get; set; }
        public byte[] FileBytes { get; set; }
        public string ContentType { get; set; }
        public string FileName { get; set; }
        public long FileSize { get; set; }
    }
}