using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;

namespace Plan
{
    /// <summary>
    /// Manages file storage for claim documents
    /// </summary>
    public static class FileStorageManager
    {
        private static readonly string baseStorageDirectory = "ClaimDocuments";
        private static readonly string fileRegistryPath = "file_registry.json";
        private static readonly long maxFileSize = 52428800; // 50 MB in bytes
        private static readonly long maxImageSize = 10485760; // 10 MB in bytes

        // Allowed file extensions
        private static readonly string[] allowedDocumentExtensions = { ".pdf", ".doc", ".docx", ".xls", ".xlsx", ".ppt", ".pptx" };
        private static readonly string[] allowedImageExtensions = { ".jpg", ".jpeg", ".png", ".gif", ".bmp" };

        static FileStorageManager()
        {
            // Ensure base storage directory exists
            if (!Directory.Exists(baseStorageDirectory))
            {
                Directory.CreateDirectory(baseStorageDirectory);
            }
        }

        #region File Storage Operations

        /// <summary>
        /// Save a file to storage and return its storage path
        /// </summary>
        public static string SaveFile(string sourceFilePath, string claimId)
        {
            try
            {
                // Validate file
                ValidateFile(sourceFilePath);

                // Create claim-specific directory
                string claimDirectory = Path.Combine(baseStorageDirectory, claimId);
                if (!Directory.Exists(claimDirectory))
                {
                    Directory.CreateDirectory(claimDirectory);
                }

                // Generate unique filename to prevent conflicts
                string originalFileName = Path.GetFileName(sourceFilePath);
                string fileExtension = Path.GetExtension(sourceFilePath);
                string fileNameWithoutExtension = Path.GetFileNameWithoutExtension(sourceFilePath);
                string uniqueFileName = $"{fileNameWithoutExtension}_{DateTime.Now:yyyyMMddHHmmss}{fileExtension}";
                string destinationPath = Path.Combine(claimDirectory, uniqueFileName);

                // Copy file to storage
                File.Copy(sourceFilePath, destinationPath, true);

                // Register file in registry
                RegisterFile(new FileRegistryEntry
                {
                    FileId = Guid.NewGuid().ToString(),
                    ClaimId = claimId,
                    OriginalFileName = originalFileName,
                    StoredFileName = uniqueFileName,
                    StoragePath = destinationPath,
                    FileSize = new FileInfo(sourceFilePath).Length,
                    UploadDate = DateTime.Now,
                    FileType = GetFileType(fileExtension)
                });

                return destinationPath;
            }
            catch (Exception ex)
            {
                throw new Exception($"Error saving file: {ex.Message}");
            }
        }

        /// <summary>
        /// Save multiple files for a claim
        /// </summary>
        public static List<string> SaveMultipleFiles(List<string> sourceFilePaths, string claimId)
        {
            var savedPaths = new List<string>();

            foreach (var filePath in sourceFilePaths)
            {
                try
                {
                    string savedPath = SaveFile(filePath, claimId);
                    savedPaths.Add(savedPath);
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Failed to save file {filePath}: {ex.Message}");
                    // Continue with other files
                }
            }

            return savedPaths;
        }

        /// <summary>
        /// Get all files for a specific claim
        /// </summary>
        public static List<FileRegistryEntry> GetClaimFiles(string claimId)
        {
            var registry = LoadFileRegistry();
            return registry.Where(f => f.ClaimId == claimId).ToList();
        }

        /// <summary>
        /// Delete a specific file
        /// </summary>
        public static bool DeleteFile(string fileId)
        {
            try
            {
                var registry = LoadFileRegistry();
                var fileEntry = registry.FirstOrDefault(f => f.FileId == fileId);

                if (fileEntry != null)
                {
                    // Delete physical file
                    if (File.Exists(fileEntry.StoragePath))
                    {
                        File.Delete(fileEntry.StoragePath);
                    }

                    // Remove from registry
                    registry.Remove(fileEntry);
                    SaveFileRegistry(registry);

                    return true;
                }

                return false;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error deleting file: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Delete all files associated with a claim
        /// </summary>
        public static void DeleteClaimFiles(string claimId)
        {
            try
            {
                var claimFiles = GetClaimFiles(claimId);

                foreach (var file in claimFiles)
                {
                    DeleteFile(file.FileId);
                }

                // Delete claim directory if empty
                string claimDirectory = Path.Combine(baseStorageDirectory, claimId);
                if (Directory.Exists(claimDirectory) && !Directory.EnumerateFileSystemEntries(claimDirectory).Any())
                {
                    Directory.Delete(claimDirectory);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error deleting claim files: {ex.Message}");
            }
        }

        /// <summary>
        /// Open/view a file
        /// </summary>
        public static void OpenFile(string fileId)
        {
            try
            {
                var registry = LoadFileRegistry();
                var fileEntry = registry.FirstOrDefault(f => f.FileId == fileId);

                if (fileEntry != null && File.Exists(fileEntry.StoragePath))
                {
                    System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                    {
                        FileName = fileEntry.StoragePath,
                        UseShellExecute = true
                    });
                }
                else
                {
                    throw new FileNotFoundException("File not found in storage.");
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"Error opening file: {ex.Message}");
            }
        }

        /// <summary>
        /// Get file info by storage path (for backward compatibility)
        /// </summary>
        public static FileRegistryEntry GetFileByPath(string storagePath)
        {
            var registry = LoadFileRegistry();
            return registry.FirstOrDefault(f => f.StoragePath == storagePath);
        }

        #endregion

        #region Validation

        private static void ValidateFile(string filePath)
        {
            if (!File.Exists(filePath))
            {
                throw new FileNotFoundException("File does not exist.");
            }

            var fileInfo = new FileInfo(filePath);
            var extension = fileInfo.Extension.ToLower();

            // Check if file type is allowed
            bool isDocument = allowedDocumentExtensions.Contains(extension);
            bool isImage = allowedImageExtensions.Contains(extension);

            if (!isDocument && !isImage)
            {
                throw new InvalidOperationException($"File type {extension} is not allowed. " +
                    $"Allowed types: {string.Join(", ", allowedDocumentExtensions.Concat(allowedImageExtensions))}");
            }

            // Check file size
            if (isDocument && fileInfo.Length > maxFileSize)
            {
                throw new InvalidOperationException($"Document file size exceeds maximum allowed size of 50 MB. " +
                    $"File size: {fileInfo.Length / 1048576.0:F2} MB");
            }

            if (isImage && fileInfo.Length > maxImageSize)
            {
                throw new InvalidOperationException($"Image file size exceeds maximum allowed size of 10 MB. " +
                    $"File size: {fileInfo.Length / 1048576.0:F2} MB");
            }
        }

        private static string GetFileType(string extension)
        {
            extension = extension.ToLower();

            if (allowedDocumentExtensions.Contains(extension))
                return "Document";
            if (allowedImageExtensions.Contains(extension))
                return "Image";

            return "Unknown";
        }

        #endregion

        #region File Registry Management

        private static List<FileRegistryEntry> LoadFileRegistry()
        {
            try
            {
                if (File.Exists(fileRegistryPath))
                {
                    string jsonContent = File.ReadAllText(fileRegistryPath);
                    return JsonConvert.DeserializeObject<List<FileRegistryEntry>>(jsonContent) ?? new List<FileRegistryEntry>();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading file registry: {ex.Message}");
            }

            return new List<FileRegistryEntry>();
        }

        private static void SaveFileRegistry(List<FileRegistryEntry> registry)
        {
            try
            {
                string jsonContent = JsonConvert.SerializeObject(registry, Formatting.Indented);
                File.WriteAllText(fileRegistryPath, jsonContent);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error saving file registry: {ex.Message}");
            }
        }

        private static void RegisterFile(FileRegistryEntry entry)
        {
            var registry = LoadFileRegistry();
            registry.Add(entry);
            SaveFileRegistry(registry);
        }

        #endregion

        #region Utility Methods

        /// <summary>
        /// Get storage statistics
        /// </summary>
        public static StorageStats GetStorageStatistics()
        {
            var registry = LoadFileRegistry();
            var stats = new StorageStats
            {
                TotalFiles = registry.Count,
                TotalSizeBytes = registry.Sum(f => f.FileSize),
                FilesByType = registry.GroupBy(f => f.FileType)
                    .ToDictionary(g => g.Key, g => g.Count()),
                FilesByClaim = registry.GroupBy(f => f.ClaimId)
                    .ToDictionary(g => g.Key, g => g.Count())
            };

            return stats;
        }

        /// <summary>
        /// Format file size for display
        /// </summary>
        public static string FormatFileSize(long bytes)
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

        /// <summary>
        /// Clean up orphaned files (files not linked to any claim)
        /// </summary>
        public static int CleanupOrphanedFiles()
        {
            int cleanedCount = 0;

            try
            {
                var registry = LoadFileRegistry();
                var allClaims = DataManager.LoadClaims();
                var validClaimIds = allClaims.Select(c => c.ClaimId).ToHashSet();

                var orphanedFiles = registry.Where(f => !validClaimIds.Contains(f.ClaimId)).ToList();

                foreach (var orphanedFile in orphanedFiles)
                {
                    if (DeleteFile(orphanedFile.FileId))
                    {
                        cleanedCount++;
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error cleaning orphaned files: {ex.Message}");
            }

            return cleanedCount;
        }

        #endregion
    }

    #region Data Models

    /// <summary>
    /// File registry entry for tracking stored files
    /// </summary>
    public class FileRegistryEntry
    {
        public string FileId { get; set; }
        public string ClaimId { get; set; }
        public string OriginalFileName { get; set; }
        public string StoredFileName { get; set; }
        public string StoragePath { get; set; }
        public long FileSize { get; set; }
        public DateTime UploadDate { get; set; }
        public string FileType { get; set; }
    }

    /// <summary>
    /// Storage statistics
    /// </summary>
    public class StorageStats
    {
        public int TotalFiles { get; set; }
        public long TotalSizeBytes { get; set; }
        public Dictionary<string, int> FilesByType { get; set; }
        public Dictionary<string, int> FilesByClaim { get; set; }

        public string TotalSizeFormatted => FileStorageManager.FormatFileSize(TotalSizeBytes);
    }

    #endregion
}