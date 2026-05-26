using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace EAPD7111Part2POE.Services
{
    public class FileStorageService : IFileStorageService
    {
        private readonly IWebHostEnvironment _environment;
        private readonly ILogger<FileStorageService> _logger;
        private readonly string[] _allowedExtensions = { ".pdf" };
        private const long _maxFileSize = 10 * 1024 * 1024;  

        public FileStorageService(IWebHostEnvironment environment, ILogger<FileStorageService> logger)
        {
            _environment = environment;
            _logger = logger;
        }

        public async Task<FileUploadResult> SaveFileAsync(IFormFile file, string subDirectory = "contracts")
        {
            var result = new FileUploadResult();

            try
            {
                if (file == null || file.Length == 0)
                {
                    result.Success = false;
                    result.ErrorMessage = "No file provided or file is empty";
                    return result;
                }

                if (file.Length > _maxFileSize)
                {
                    result.Success = false;
                    result.ErrorMessage = $"File size exceeds the maximum allowed size of {_maxFileSize / 1024 / 1024}MB";
                    return result;
                }

                var fileExtension = Path.GetExtension(file.FileName).ToLowerInvariant();
                if (!_allowedExtensions.Contains(fileExtension))
                {
                    result.Success = false;
                    result.ErrorMessage = "Only PDF files are allowed";
                    return result;
                }

                if (file.ContentType != "application/pdf")
                {
                    result.Success = false;
                    result.ErrorMessage = "Invalid file type. Only PDF files are accepted.";
                    return result;
                }

                var uploadsFolder = Path.Combine(_environment.WebRootPath, "uploads", subDirectory);
                if (!Directory.Exists(uploadsFolder))
                {
                    Directory.CreateDirectory(uploadsFolder);
                }

                var uniqueFileName = $"{Guid.NewGuid()}_{SanitizeFileName(Path.GetFileNameWithoutExtension(file.FileName))}{fileExtension}";
                var filePath = Path.Combine(uploadsFolder, uniqueFileName);

                var tempFilePath = filePath + ".tmp";
                try
                {
                    using (var fileStream = new FileStream(tempFilePath, FileMode.Create, FileAccess.Write, FileShare.None, 8192, useAsync: true))
                    {
                        await file.CopyToAsync(fileStream);
                        await fileStream.FlushAsync();
                    }

                    if (File.Exists(filePath))
                    {
                        File.Delete(filePath);
                    }
                    File.Move(tempFilePath, filePath);
                }
                finally
                {
                    if (File.Exists(tempFilePath))
                    {
                        File.Delete(tempFilePath);
                    }
                }

                var relativePath = Path.Combine("uploads", subDirectory, uniqueFileName).Replace("\\", "/");

                result.Success = true;
                result.FilePath = $"/{relativePath}";
                result.FileName = uniqueFileName;
                result.OriginalFileName = file.FileName;
                result.FileSize = file.Length;
                result.ContentType = file.ContentType;

                _logger.LogInformation($"File saved successfully: {uniqueFileName} (Original: {file.FileName})");
            }
            catch (IOException ex)
            {
                _logger.LogError(ex, "IO Error saving file");
                result.Success = false;
                result.ErrorMessage = "An error occurred while saving the file. Please try again.";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving file");
                result.Success = false;
                result.ErrorMessage = "An unexpected error occurred while saving the file.";
            }

            return result;
        }

        public bool DeleteFile(string filePath)
        {
            try
            {
                if (string.IsNullOrEmpty(filePath))
                    return false;

                var relativePath = filePath.TrimStart('/');
                var fullPath = Path.Combine(_environment.WebRootPath, relativePath);

                if (File.Exists(fullPath))
                {
                    File.Delete(fullPath);
                    _logger.LogInformation($"File deleted successfully: {fullPath}");
                    return true;
                }

                _logger.LogWarning($"File not found for deletion: {fullPath}");
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error deleting file: {filePath}");
                return false;
            }
        }

        public async Task<FileDownloadResult> GetFileAsync(string filePath)
        {
            var result = new FileDownloadResult();

            try
            {
                if (string.IsNullOrEmpty(filePath))
                {
                    result.Success = false;
                    result.ErrorMessage = "File path is null or empty";
                    return result;
                }

                var relativePath = filePath.TrimStart('/');
                var fullPath = Path.Combine(_environment.WebRootPath, relativePath);

                if (!File.Exists(fullPath))
                {
                    result.Success = false;
                    result.ErrorMessage = "File not found";
                    return result;
                }

                var fileBytes = await File.ReadAllBytesAsync(fullPath);

                result.Success = true;
                result.FileBytes = fileBytes;
                result.ContentType = "application/pdf";
                result.FileName = Path.GetFileName(fullPath);
                result.FileSize = fileBytes.Length;

                _logger.LogInformation($"File retrieved successfully: {fullPath} ({fileBytes.Length} bytes)");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error reading file: {filePath}");
                result.Success = false;
                result.ErrorMessage = "An error occurred while reading the file";
            }

            return result;
        }

        public byte[] GetFile(string filePath)
        {
            var result = GetFileAsync(filePath).GetAwaiter().GetResult();
            return result.Success ? result.FileBytes : null;
        }

        public bool ValidateFile(IFormFile file)
        {
            if (file == null || file.Length == 0)
                return false;

            if (file.Length > _maxFileSize)
                return false;

            var fileExtension = Path.GetExtension(file.FileName).ToLowerInvariant();
            if (!_allowedExtensions.Contains(fileExtension))
                return false;

            if (file.ContentType != "application/pdf")
                return false;

            return true;
        }

        public string GetFileSizeDisplay(long bytes)
        {
            string[] sizes = { "B", "KB", "MB", "GB" };
            double len = bytes;
            int order = 0;
            while (len >= 1024 && order < sizes.Length - 1)
            {
                order++;
                len = len / 1024;
            }
            return $"{len:0.##} {sizes[order]}";
        }

        private string SanitizeFileName(string fileName)
        {
            if (string.IsNullOrEmpty(fileName))
                return "document";

            char[] invalidChars = Path.GetInvalidFileNameChars();
            foreach (char c in invalidChars)
            {
                fileName = fileName.Replace(c.ToString(), "");
            }

            fileName = fileName.Trim();
            if (fileName.Length > 50)
                fileName = fileName.Substring(0, 50);

            return string.IsNullOrEmpty(fileName) ? "document" : fileName;
        }
    }
}