using HandoraApplication.IServices;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace HandoraInfrastructure.Services
{
    public class ImageValidationService : IImageValidationService
    {
        private readonly ILogger<ImageValidationService> _logger;
        
        // Max file size: 5 MB
        private const long MaxFileSizeInBytes = 5 * 1024 * 1024;

        private static readonly Dictionary<string, byte[]> ImageSignatures = new()
        {
            { ".png",  new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A } },
            { ".jpeg", new byte[] { 0xFF, 0xD8, 0xFF } },
            { ".jpg",  new byte[] { 0xFF, 0xD8, 0xFF } },
            { ".gif",  new byte[] { 0x47, 0x49, 0x46, 0x38 } }
        };

        public ImageValidationService(ILogger<ImageValidationService> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public (bool IsValid, string ErrorMessage) ValidateImage(IFormFile file)
        {
            if (file == null)
            {
                return (false, "File cannot be null.");
            }

            if (file.Length == 0)
            {
                return (false, "Uploaded file is empty.");
            }

            if (file.Length > MaxFileSizeInBytes)
            {
                return (false, $"File size exceeds the maximum limit of {MaxFileSizeInBytes / (1024 * 1024)} MB.");
            }

            var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
            if (string.IsNullOrEmpty(extension) || !ImageSignatures.ContainsKey(extension) && extension != ".webp")
            {
                return (false, "Unsupported file extension. Only PNG, JPG, JPEG, GIF, and WEBP are allowed.");
            }

            // Security Validation: Magic Bytes Check
            try
            {
                using var stream = file.OpenReadStream();
                
                // WebP signature check is slightly different (RIFF....WEBP)
                if (extension == ".webp")
                {
                    if (file.Length < 12)
                    {
                        return (false, "Invalid WebP image format.");
                    }

                    byte[] header = new byte[12];
                    stream.Read(header, 0, 12);

                    bool isRiff = header[0] == 0x52 && header[1] == 0x49 && header[2] == 0x46 && header[3] == 0x46; // "RIFF"
                    bool isWebp = header[8] == 0x57 && header[9] == 0x45 && header[10] == 0x42 && header[11] == 0x50; // "WEBP"

                    if (!isRiff || !isWebp)
                    {
                        _logger.LogWarning("Security Warning: WebP file header magic bytes do not match standard signature.");
                        return (false, "The file header does not match a valid WebP image format.");
                    }
                }
                else
                {
                    var signature = ImageSignatures[extension];
                    byte[] header = new byte[signature.Length];
                    stream.Read(header, 0, signature.Length);

                    if (!header.SequenceEqual(signature.Take(header.Length)))
                    {
                        _logger.LogWarning("Security Warning: File extension {Extension} magic bytes do not match actual signature.", extension);
                        return (false, "The file header content does not match its extension signature.");
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while validating image magic bytes.");
                return (false, "An error occurred while validating the image security headers.");
            }

            return (true, string.Empty);
        }
    }
}
