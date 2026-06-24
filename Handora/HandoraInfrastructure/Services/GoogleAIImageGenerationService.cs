using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using HandoraApplication.AI.DTOs;
using HandoraApplication.AI.Exceptions;
using HandoraApplication.AI.Interfaces;
using HandoraInfrastructure.AI.Options;
using HandoraApplication.IServices;
using HandoraInfrastructure.Settings;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace HandoraInfrastructure.Services
{
    public class GoogleAIImageGenerationService : IAIImageGenerationService
    {
        private readonly HttpClient _httpClient;
        private readonly IFileService _fileService;
        private readonly AiImageGeneratorSettings _settings;
        private readonly GeminiOptions _geminiOptions;
        private readonly ILogger<GoogleAIImageGenerationService> _logger;

        public string ProviderName => "GoogleAIStudio";

        public GoogleAIImageGenerationService(
            HttpClient httpClient,
            IFileService fileService,
            IOptions<AiImageGeneratorSettings> settings,
            IOptions<GeminiOptions> geminiOptions,
            ILogger<GoogleAIImageGenerationService> logger)
        {
            _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
            _fileService = fileService ?? throw new ArgumentNullException(nameof(fileService));
            _settings = settings?.Value ?? throw new ArgumentNullException(nameof(settings));
            _geminiOptions = geminiOptions?.Value ?? throw new ArgumentNullException(nameof(geminiOptions));
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

            _logger.LogInformation("AI Generation request: PromptLength={PromptLength}, Provider={Provider}", request.Prompt.Length, ProviderName);

            var stopwatch = Stopwatch.StartNew();
            try
            {
                var apiKey = !string.IsNullOrWhiteSpace(_settings.GoogleAIStudio.ApiKey)
                    ? _settings.GoogleAIStudio.ApiKey
                    : _geminiOptions.ApiKey;

                if (string.IsNullOrWhiteSpace(apiKey) || apiKey.StartsWith("YOUR_") || apiKey.Contains("YOUR_GEMINI_API_KEY"))
                {
                    _logger.LogWarning("Google AI Studio API Key is missing or default. Provider is unavailable.");
                    throw new AIProviderUnavailableException("Google AI Studio API Key is not configured.");
                }

                var baseUrl = _settings.GoogleAIStudio.BaseUrl.TrimEnd('/');
                var model = _settings.GoogleAIStudio.ModelName;

                if (model == "imagen-3.0-generate-002")
                {
                    model = "imagen-4.0-generate-001";
                }

                var method = model.Contains("imagen-4") ? "predict" : "generateImages";
                var url = $"{baseUrl}/v1beta/models/{model}:{method}?key={apiKey}";

                var requestBody = new
                {
                    instances = new[]
                    {
                        new { prompt = request.Prompt }
                    },
                    parameters = new
                    {
                        sampleCount = request.ImageCount,
                        aspectRatio = "1:1"
                    }
                };

                var jsonContent = JsonSerializer.Serialize(requestBody);
                var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

                HttpResponseMessage response = await SendRequestWithRetryAsync(async () =>
                {
                    return await _httpClient.PostAsync(url, content, ct);
                }, ct);

                stopwatch.Stop();

                if (response == null)
                {
                    throw new AINetworkException("Failed to receive a response from Google AI Studio.");
                }

                if (!response.IsSuccessStatusCode)
                {
                    await HandleErrorResponseAsync(response, ct);
                }

                var responseString = await response.Content.ReadAsStringAsync(ct);
                var imagesList = ParseImagesFromResponse(responseString, request.Prompt);

                var generatedImages = new List<GeneratedImage>();
                foreach (var imgData in imagesList)
                {
                    // Upload to Cloudinary
                    using var ms = new MemoryStream(imgData);
                    var formFile = new FormFile(ms, 0, imgData.Length, "file", $"generated_{Guid.NewGuid():N}.jpg")
                    {
                        Headers = new HeaderDictionary(),
                        ContentType = "image/jpeg"
                    };

                    _logger.LogInformation("Uploading generated image to storage...");
                    var imageUrl = await _fileService.UploadFileAsync(formFile, "custom_designs");

                    generatedImages.Add(new GeneratedImage
                    {
                        ImageUrl = imageUrl,
                        RevisedPrompt = request.Prompt
                    });
                }

                _logger.LogInformation("AI Generation success. DurationMs={DurationMs}, Provider={Provider}", stopwatch.ElapsedMilliseconds, ProviderName);

                return new GenerateImageResponse
                {
                    Images = generatedImages,
                    IsSuccess = true,
                    Metadata = new GenerationMetadata
                    {
                        ProviderName = ProviderName,
                        ModelName = model,
                        DurationMs = stopwatch.ElapsedMilliseconds
                    }
                };
            }
            catch (Exception ex) when (!(ex is AIException))
            {
                stopwatch.Stop();
                _logger.LogError(ex, "Unexpected error in GoogleAIImageGenerationService.");
                throw new AIException("An unexpected error occurred during image generation.", ex);
            }
        }

        public async Task<GenerateImageResponse> GenerateVariationsAsync(GenerateImageRequest request, CancellationToken ct = default)
        {
            if (request == null)
            {
                throw new ArgumentNullException(nameof(request));
            }
            if (string.IsNullOrEmpty(request.BaseImageUrl))
            {
                throw new AIInvalidImageException("Base image URL is required for generating variations.");
            }

            // Google AI Studio Imagen doesn't natively support image-to-image variation, 
            // so we adapt the text prompt to describe the variation based on similarity strength.
            var adaptedRequest = new GenerateImageRequest
            {
                Prompt = $"{request.Prompt} (A variation inspired by style reference: {request.BaseImageUrl})",
                NegativePrompt = request.NegativePrompt,
                ImageCount = request.ImageCount,
                BypassCache = request.BypassCache,
                UserId = request.UserId
            };

            var response = await GenerateImageAsync(adaptedRequest, ct);

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
                throw new AIInvalidImageException("Base image URL is required for image refinement.");
            }

            // Google AI Studio Imagen doesn't natively support image-to-image editing/refinement,
            // so we adapt the text prompt for high-fidelity description.
            var adaptedRequest = new GenerateImageRequest
            {
                Prompt = $"{request.Prompt} (Refined version of style and layout reference: {request.BaseImageUrl})",
                NegativePrompt = request.NegativePrompt,
                ImageCount = request.ImageCount,
                BypassCache = request.BypassCache,
                UserId = request.UserId
            };

            var response = await GenerateImageAsync(adaptedRequest, ct);

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
            // Quota and feature limits are verified at application/domain levels.
            // Return true indicating the provider itself has no active software generation constraints configured for this caller.
            return Task.FromResult(true);
        }

        public async Task<AIHealthCheckResult> CheckHealthAsync(CancellationToken ct = default)
        {
            var apiKey = !string.IsNullOrWhiteSpace(_settings.GoogleAIStudio.ApiKey)
                ? _settings.GoogleAIStudio.ApiKey
                : _geminiOptions.ApiKey;

            if (string.IsNullOrWhiteSpace(apiKey) || apiKey.StartsWith("YOUR_") || apiKey.Contains("YOUR_GEMINI_API_KEY"))
            {
                return new AIHealthCheckResult
                {
                    IsHealthy = false,
                    ProviderName = ProviderName,
                    ModelName = _settings.GoogleAIStudio.ModelName,
                    Status = "Degraded",
                    Details = "API Key is missing or not configured."
                };
            }

            var baseUrl = _settings.GoogleAIStudio.BaseUrl.TrimEnd('/');
            var url = $"{baseUrl}/v1beta/models?key={apiKey}";

            try
            {
                var response = await _httpClient.GetAsync(url, ct);
                if (response.IsSuccessStatusCode)
                {
                    return new AIHealthCheckResult
                    {
                        IsHealthy = true,
                        ProviderName = ProviderName,
                        ModelName = _settings.GoogleAIStudio.ModelName,
                        Status = "Healthy",
                        Details = "Reachable, authenticated successfully, models listed."
                    };
                }

                var errorMsg = await response.Content.ReadAsStringAsync(ct);
                return new AIHealthCheckResult
                {
                    IsHealthy = false,
                    ProviderName = ProviderName,
                    ModelName = _settings.GoogleAIStudio.ModelName,
                    Status = "Unhealthy",
                    Details = $"Google API responded with error status {response.StatusCode}: {errorMsg}"
                };
            }
            catch (Exception ex)
            {
                return new AIHealthCheckResult
                {
                    IsHealthy = false,
                    ProviderName = ProviderName,
                    ModelName = _settings.GoogleAIStudio.ModelName,
                    Status = "Unhealthy",
                    Details = $"Failed to reach Google AI Studio: {ex.Message}"
                };
            }
        }

        private async Task<HttpResponseMessage> SendRequestWithRetryAsync(Func<Task<HttpResponseMessage>> sendFunc, CancellationToken ct)
        {
            const int maxRetries = 3;
            const int delayBaseMs = 1000;
            HttpResponseMessage response = null!;

            for (int attempt = 1; attempt <= maxRetries; attempt++)
            {
                try
                {
                    response = await sendFunc();
                    if (response.IsSuccessStatusCode)
                    {
                        return response;
                    }

                    var errorMsg = await response.Content.ReadAsStringAsync(ct);
                    _logger.LogWarning("Google AI Studio response failed on attempt {Attempt}/{MaxRetries}. Status: {StatusCode}, Error: {Error}", 
                        attempt, maxRetries, response.StatusCode, errorMsg);

                    if (attempt < maxRetries && (response.StatusCode == System.Net.HttpStatusCode.TooManyRequests || (int)response.StatusCode >= 500))
                    {
                        var delay = delayBaseMs * (int)Math.Pow(2, attempt - 1);
                        _logger.LogInformation("Waiting {DelayMs}ms before retrying generation request...", delay);
                        await Task.Delay(delay, ct);
                        continue;
                    }
                    break;
                }
                catch (HttpRequestException ex) when (attempt < maxRetries)
                {
                    _logger.LogWarning(ex, "Network error on attempt {Attempt}/{MaxRetries} of image generation request. Retrying...", attempt, maxRetries);
                    var delay = delayBaseMs * (int)Math.Pow(2, attempt - 1);
                    await Task.Delay(delay, ct);
                }
                catch (TaskCanceledException ex) when (!ct.IsCancellationRequested)
                {
                    _logger.LogWarning(ex, "Timeout on attempt {Attempt}/{MaxRetries} of image generation request. Retrying...", attempt, maxRetries);
                    var delay = delayBaseMs * (int)Math.Pow(2, attempt - 1);
                    await Task.Delay(delay, ct);
                }
            }
            return response;
        }

        private async Task HandleErrorResponseAsync(HttpResponseMessage response, CancellationToken ct)
        {
            var content = await response.Content.ReadAsStringAsync(ct);
            var statusCode = response.StatusCode;

            if (statusCode == System.Net.HttpStatusCode.TooManyRequests)
            {
                if (content.Contains("quota", StringComparison.OrdinalIgnoreCase) || content.Contains("limit", StringComparison.OrdinalIgnoreCase))
                {
                    throw new AIQuotaExceededException("AI Provider quota limit exceeded. Please try again later.");
                }
                throw new AIRateLimitException("Too many requests sent to the AI Provider. Rate limit reached.");
            }
            else if (statusCode == System.Net.HttpStatusCode.BadRequest)
            {
                if (content.Contains("safety", StringComparison.OrdinalIgnoreCase) || content.Contains("block", StringComparison.OrdinalIgnoreCase))
                {
                    throw new AIInvalidPromptException("The request prompt was flagged and blocked by the safety filters.");
                }
                throw new AIInvalidPromptException($"The image generation request was invalid: {content}");
            }
            else if (statusCode == System.Net.HttpStatusCode.GatewayTimeout || statusCode == System.Net.HttpStatusCode.RequestTimeout)
            {
                throw new AITimeoutException("The AI Provider request timed out.");
            }
            else if ((int)statusCode >= 500)
            {
                throw new AIProviderUnavailableException($"AI Provider service is currently unavailable (Status: {statusCode}).");
            }
            
            throw new AIException($"AI Generation failed with status {statusCode}: {content}");
        }

        private List<byte[]> ParseImagesFromResponse(string responseString, string prompt)
        {
            using var jsonDoc = JsonDocument.Parse(responseString);
            var root = jsonDoc.RootElement;
            var imagesList = new List<byte[]>();

            if (root.TryGetProperty("predictions", out var predictions) && predictions.GetArrayLength() > 0)
            {
                foreach (var prediction in predictions.EnumerateArray())
                {
                    if (prediction.TryGetProperty("bytesBase64Encoded", out var bytesProp))
                    {
                        var base64Bytes = bytesProp.GetString();
                        if (!string.IsNullOrEmpty(base64Bytes))
                        {
                            imagesList.Add(Convert.FromBase64String(base64Bytes));
                        }
                    }
                }
            }
            else if (root.TryGetProperty("generatedImages", out var generatedImages) && generatedImages.GetArrayLength() > 0)
            {
                foreach (var imgObj in generatedImages.EnumerateArray())
                {
                    if (imgObj.TryGetProperty("image", out var imageNode) && imageNode.TryGetProperty("imageBytes", out var imageBytesProp))
                    {
                        var base64Bytes = imageBytesProp.GetString();
                        if (!string.IsNullOrEmpty(base64Bytes))
                        {
                            imagesList.Add(Convert.FromBase64String(base64Bytes));
                        }
                    }
                }
            }

            if (imagesList.Count == 0)
            {
                throw new AIException("No images returned by the provider (unknown response schema).");
            }

            return imagesList;
        }
    }
}
