using System;
using System.Collections.Generic;

namespace HandoraApplication.AI.DTOs
{
    public class GenerateImageRequest
    {
        public string Prompt { get; set; } = string.Empty;
        public string NegativePrompt { get; set; } = string.Empty;
        public int ImageCount { get; set; } = 1;
        public string? BaseImageUrl { get; set; }
        public double SimilarityWeight { get; set; } = 0.5; // Strength for variation/refinement
        public bool BypassCache { get; set; } = false;
        public string? UserId { get; set; }
    }

    public class GenerateImageResponse
    {
        public List<GeneratedImage> Images { get; set; } = new();
        public GenerationMetadata Metadata { get; set; } = new();
        public bool IsSuccess { get; set; }
        public string? ErrorMessage { get; set; }
    }

    public class GeneratedImage
    {
        public string ImageUrl { get; set; } = string.Empty;
        public string? RevisedPrompt { get; set; }
        public ImageVariation? BaseImageVariation { get; set; }
    }

    public class GenerationMetadata
    {
        public string ProviderName { get; set; } = string.Empty;
        public string ModelName { get; set; } = string.Empty;
        public long DurationMs { get; set; }
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    }

    public class ImageVariation
    {
        public string BaseImageUrl { get; set; } = string.Empty;
        public double Strength { get; set; }
    }

    public class PromptBuildResult
    {
        public string PositivePrompt { get; set; } = string.Empty;
        public string NegativePrompt { get; set; } = string.Empty;
    }

    public class AIHealthCheckResult
    {
        public bool IsHealthy { get; set; }
        public string ProviderName { get; set; } = string.Empty;
        public string ModelName { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public string Details { get; set; } = string.Empty;
    }
}
