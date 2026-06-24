using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using HandoraApplication.AI.DTOs;
using HandoraApplication.AI.Interfaces;
using HandoraInfrastructure.Settings;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace HandoraInfrastructure.Services
{
    public class MockAIImageGenerationService : IAIImageGenerationService
    {
        private readonly AiImageGeneratorSettings _settings;
        private readonly ILogger<MockAIImageGenerationService> _logger;

        public string ProviderName => "MockProvider";

        public MockAIImageGenerationService(
            IOptions<AiImageGeneratorSettings> settings,
            ILogger<MockAIImageGenerationService> logger)
        {
            _settings = settings?.Value ?? throw new ArgumentNullException(nameof(settings));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        private static int _counter = 0;
        private static readonly string[] MockImages = new[]
        {
            "https://images.unsplash.com/photo-1615486511484-92e172cc4fe0?auto=format&fit=crop&q=80&w=600", // Dino
            "https://images.unsplash.com/photo-1608096299210-db7e38487075?auto=format&fit=crop&q=80&w=600", // Teddy Bear
            "https://images.unsplash.com/photo-1556257211-330026e34346?auto=format&fit=crop&q=80&w=600", // Bunny
            "https://images.unsplash.com/photo-1612538498456-e861df91d4d0?auto=format&fit=crop&q=80&w=600", // Kitty/Cat
            "https://images.unsplash.com/photo-1561464965-9480d383f7e0?auto=format&fit=crop&q=80&w=600", // Octopus
            "https://images.unsplash.com/photo-1584917865442-de89df76afd3?auto=format&fit=crop&q=80&w=600"  // General doll
        };

        public Task<GenerateImageResponse> GenerateImageAsync(GenerateImageRequest request, CancellationToken ct = default)
        {
            if (request == null)
            {
                throw new ArgumentNullException(nameof(request));
            }

            _logger.LogInformation("Generating mock images. PromptLength={PromptLength}, ImageCount={ImageCount}", request.Prompt.Length, request.ImageCount);

            var images = new List<GeneratedImage>();
            for (int i = 0; i < request.ImageCount; i++)
            {
                int index = Interlocked.Increment(ref _counter) % MockImages.Length;
                images.Add(new GeneratedImage
                {
                    ImageUrl = MockImages[index],
                    RevisedPrompt = request.Prompt
                });
            }

            var response = new GenerateImageResponse
            {
                Images = images,
                IsSuccess = true,
                Metadata = new GenerationMetadata
                {
                    ProviderName = ProviderName,
                    ModelName = "Mock-Model-v1",
                    DurationMs = 150
                }
            };

            return Task.FromResult(response);
        }

        public async Task<GenerateImageResponse> GenerateVariationsAsync(GenerateImageRequest request, CancellationToken ct = default)
        {
            if (request == null)
            {
                throw new ArgumentNullException(nameof(request));
            }
            if (string.IsNullOrEmpty(request.BaseImageUrl))
            {
                throw new ArgumentException("Base image URL is required for variations.", nameof(request));
            }

            _logger.LogInformation("Generating mock image variation for base image: {BaseImageUrl}", request.BaseImageUrl);

            var response = await GenerateImageAsync(request, ct);

            foreach (var img in response.Images)
            {
                img.BaseImageVariation = new ImageVariation
                {
                    BaseImageUrl = request.BaseImageUrl,
                    Strength = request.SimilarityWeight
                };
            }

            return response;
        }

        public async Task<GenerateImageResponse> RefineImageAsync(GenerateImageRequest request, CancellationToken ct = default)
        {
            if (request == null)
            {
                throw new ArgumentNullException(nameof(request));
            }
            if (string.IsNullOrEmpty(request.BaseImageUrl))
            {
                throw new ArgumentException("Base image URL is required for refinement.", nameof(request));
            }

            _logger.LogInformation("Generating mock image refinement for base image: {BaseImageUrl}", request.BaseImageUrl);

            var response = await GenerateImageAsync(request, ct);

            foreach (var img in response.Images)
            {
                img.BaseImageVariation = new ImageVariation
                {
                    BaseImageUrl = request.BaseImageUrl,
                    Strength = request.SimilarityWeight
                };
            }

            return response;
        }

        public Task<bool> ValidateLimitsAsync(string userId, CancellationToken ct = default)
        {
            return Task.FromResult(true);
        }

        public Task<AIHealthCheckResult> CheckHealthAsync(CancellationToken ct = default)
        {
            return Task.FromResult(new AIHealthCheckResult
            {
                IsHealthy = true,
                ProviderName = ProviderName,
                ModelName = "Mock-Model-v1",
                Status = "Healthy",
                Details = "Mock provider active and healthy."
            });
        }
    }
}
