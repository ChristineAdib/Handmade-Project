using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using HandoraApplication.AI.DTOs;
using HandoraApplication.AI.Exceptions;
using HandoraApplication.AI.Interfaces;
using HandoraApplication.IServices;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace HandoraInfrastructure.Services
{
    public class PollinationsAIImageGenerationService : IAIImageGenerationService
    {
        private readonly HttpClient _httpClient;
        private readonly IFileService _fileService;
        private readonly ILogger<PollinationsAIImageGenerationService> _logger;
        private static readonly Random _random = new Random();

        public string ProviderName => "Pollinations.ai";

        public PollinationsAIImageGenerationService(
            HttpClient httpClient,
            IFileService fileService,
            ILogger<PollinationsAIImageGenerationService> logger)
        {
            _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
            _fileService = fileService ?? throw new ArgumentNullException(nameof(fileService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<GenerateImageResponse> GenerateImageAsync(GenerateImageRequest request, CancellationToken ct = default)
        {
            if (request == null)
            {
                throw new ArgumentNullException(nameof(request));
            }
            if (string.IsNullOrWhiteSpace(request.Prompt))
            {
                throw new AIInvalidPromptException("Prompt cannot be empty.");
            }

            _logger.LogInformation("Pollinations AI image generation started: PromptLength={PromptLength}", request.Prompt.Length);

            var stopwatch = Stopwatch.StartNew();
            try
            {
                var generatedImages = new List<GeneratedImage>();

                for (int i = 0; i < request.ImageCount; i++)
                {
                    // Generate a random seed if none is specified or if we are generating multiple variations
                    int seed = _random.Next(1, 100000000);
                    var encodedPrompt = Uri.EscapeDataString(request.Prompt);
                    
                    // Build base URL
                    var url = $"https://image.pollinations.ai/prompt/{encodedPrompt}?model=flux&width=1024&height=1280&seed={seed}&nologo=true";

                    // Append image if we are doing img2img/refinement
                    if (!string.IsNullOrEmpty(request.BaseImageUrl))
                    {
                        url += $"&image={Uri.EscapeDataString(request.BaseImageUrl)}";
                    }

                    _logger.LogInformation("Calling Pollinations.ai API: {Url}", url);

                    // Fetch image bytes
                    var imgData = await _httpClient.GetByteArrayAsync(url, ct);

                    if (imgData == null || imgData.Length == 0)
                    {
                        throw new AINetworkException("Pollinations.ai returned empty image bytes.");
                    }

                    // Upload to Cloudinary using FileService
                    using var ms = new MemoryStream(imgData);
                    var formFile = new FormFile(ms, 0, imgData.Length, "file", $"generated_{Guid.NewGuid():N}.jpg")
                    {
                        Headers = new HeaderDictionary(),
                        ContentType = "image/jpeg"
                    };

                    _logger.LogInformation("Uploading Pollinations image bytes to Cloudinary...");
                    var imageUrl = await _fileService.UploadFileAsync(formFile, "custom_designs");

                    var generatedImage = new GeneratedImage
                    {
                        ImageUrl = imageUrl,
                        RevisedPrompt = request.Prompt
                    };

                    if (!string.IsNullOrEmpty(request.BaseImageUrl))
                    {
                        generatedImage.BaseImageVariation = new ImageVariation
                        {
                            BaseImageUrl = request.BaseImageUrl,
                            Strength = request.SimilarityWeight
                        };
                    }

                    generatedImages.Add(generatedImage);
                }

                stopwatch.Stop();
                _logger.LogInformation("Pollinations AI generation completed in {DurationMs}ms.", stopwatch.ElapsedMilliseconds);

                return new GenerateImageResponse
                {
                    Images = generatedImages,
                    IsSuccess = true,
                    Metadata = new GenerationMetadata
                    {
                        ProviderName = ProviderName,
                        ModelName = "flux",
                        DurationMs = stopwatch.ElapsedMilliseconds,
                        Timestamp = DateTime.UtcNow
                    }
                };
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                _logger.LogError(ex, "Error in PollinationsAIImageGenerationService.");
                return new GenerateImageResponse
                {
                    Images = new List<GeneratedImage>(),
                    IsSuccess = false,
                    ErrorMessage = $"Pollinations AI generation failed: {ex.Message}",
                    Metadata = new GenerationMetadata
                    {
                        ProviderName = ProviderName,
                        ModelName = "flux",
                        DurationMs = stopwatch.ElapsedMilliseconds,
                        Timestamp = DateTime.UtcNow
                    }
                };
            }
        }

        public Task<GenerateImageResponse> GenerateVariationsAsync(GenerateImageRequest request, CancellationToken ct = default)
        {
            if (request == null)
            {
                throw new ArgumentNullException(nameof(request));
            }
            if (string.IsNullOrEmpty(request.BaseImageUrl))
            {
                throw new ArgumentException("Base image URL is required for variations.", nameof(request.BaseImageUrl));
            }

            // In Pollinations, variation is done using the base image parameter
            return GenerateImageAsync(request, ct);
        }

        public Task<GenerateImageResponse> RefineImageAsync(GenerateImageRequest request, CancellationToken ct = default)
        {
            if (request == null)
            {
                throw new ArgumentNullException(nameof(request));
            }
            if (string.IsNullOrEmpty(request.BaseImageUrl))
            {
                throw new ArgumentException("Base image URL is required for refinement.", nameof(request.BaseImageUrl));
            }

            return GenerateImageAsync(request, ct);
        }

        public Task<bool> ValidateLimitsAsync(string userId, CancellationToken ct = default)
        {
            // Free and keyless endpoint, no limits enforced locally
            return Task.FromResult(true);
        }

        public async Task<AIHealthCheckResult> CheckHealthAsync(CancellationToken ct = default)
        {
            try
            {
                // Simple request to see if the server responds
                var response = await _httpClient.GetAsync("https://image.pollinations.ai/prompt/healthcheck?model=flux&width=16&height=16", ct);
                return new AIHealthCheckResult
                {
                    IsHealthy = response.IsSuccessStatusCode,
                    ProviderName = ProviderName,
                    ModelName = "flux",
                    Status = response.IsSuccessStatusCode ? "Healthy" : "Degraded",
                    Details = $"Ping returned status code {response.StatusCode}"
                };
            }
            catch (Exception ex)
            {
                return new AIHealthCheckResult
                {
                    IsHealthy = false,
                    ProviderName = ProviderName,
                    ModelName = "flux",
                    Status = "Unhealthy",
                    Details = ex.Message
                };
            }
        }
    }
}
