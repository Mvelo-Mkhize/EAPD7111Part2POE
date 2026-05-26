using Microsoft.AspNetCore.Http;
using System.Threading.Tasks;

namespace EAPD7111Part2POE.Services
{
    public interface IFileStorageService
    {
        Task<FileUploadResult> SaveFileAsync(IFormFile file, string subDirectory = "contracts");

        bool DeleteFile(string filePath);

        Task<FileDownloadResult> GetFileAsync(string filePath);

        byte[] GetFile(string filePath);

        bool ValidateFile(IFormFile file);

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