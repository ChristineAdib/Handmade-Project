using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using HandoraApplication.AI.DTOs;
using HandoraApplication.AI.Interfaces;
using HandoraInfrastructure.Settings;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace HandoraInfrastructure.Services
{
    public class GenerationQualityValidator : IGenerationQualityValidator
    {
        private readonly AiImageGeneratorSettings _settings;
        private readonly ILogger<GenerationQualityValidator> _logger;

        // JPEG magic bytes: FF D8 FF
        private static readonly byte[] JpegSignature = { 0xFF, 0xD8, 0xFF };
        // PNG magic bytes: 89 50 4E 47 0D 0A 1A 0A
        private static readonly byte[] PngSignature = { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A };

        public GenerationQualityValidator(
            IOptions<AiImageGeneratorSettings> settings,
            ILogger<GenerationQualityValidator> logger)
        {
            _settings = settings?.Value ?? throw new ArgumentNullException(nameof(settings));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public Task<QualityValidationResult> ValidateAsync(byte[] imageData, string prompt, CancellationToken ct = default)
        {
            if (!_settings.EnableQualityValidation)
            {
                return Task.FromResult(new QualityValidationResult
                {
                    IsAcceptable = true,
                    QualityScore = 100.0
                });
            }

            // Check 1: Non-null and non-empty
            if (imageData == null || imageData.Length == 0)
            {
                _logger.LogWarning("Quality validation failed: Image data is null or empty.");
                return Task.FromResult(new QualityValidationResult
                {
                    IsAcceptable = false,
                    RejectionReason = "Generated image is empty or null.",
                    QualityScore = 0
                });
            }

            // Check 2: Minimum file size (catches blank/corrupted images)
            if (imageData.Length < _settings.MinImageSizeBytes)
            {
                _logger.LogWarning("Quality validation failed: Image size {Size} bytes is below minimum {Min} bytes.",
                    imageData.Length, _settings.MinImageSizeBytes);
                return Task.FromResult(new QualityValidationResult
                {
                    IsAcceptable = false,
                    RejectionReason = $"Generated image is too small ({imageData.Length} bytes). Minimum required: {_settings.MinImageSizeBytes} bytes.",
                    QualityScore = 10
                });
            }

            // Check 3: Valid image format (JPEG or PNG magic bytes)
            bool isValidJpeg = imageData.Length >= JpegSignature.Length &&
                               imageData.Take(JpegSignature.Length).SequenceEqual(JpegSignature);
            bool isValidPng = imageData.Length >= PngSignature.Length &&
                              imageData.Take(PngSignature.Length).SequenceEqual(PngSignature);

            if (!isValidJpeg && !isValidPng)
            {
                _logger.LogWarning("Quality validation failed: Image does not have valid JPEG or PNG header.");
                return Task.FromResult(new QualityValidationResult
                {
                    IsAcceptable = false,
                    RejectionReason = "Generated image has an invalid format (not JPEG or PNG).",
                    QualityScore = 5
                });
            }

            // All checks passed
            _logger.LogInformation("Quality validation passed: Image size={Size} bytes, format={Format}.",
                imageData.Length, isValidJpeg ? "JPEG" : "PNG");

            return Task.FromResult(new QualityValidationResult
            {
                IsAcceptable = true,
                QualityScore = 95.0
            });
        }
    }
}
