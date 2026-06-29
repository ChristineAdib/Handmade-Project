using System;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using HandoraApplication.AI.DTOs;
using HandoraApplication.AI.Interfaces;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

namespace HandoraInfrastructure.Services
{
    public class AIImageGenerationCacheDecorator : IAIImageGenerationService
    {
        private readonly IAIImageGenerationService _innerService;
        private readonly IMemoryCache _cache;
        private readonly ILogger<AIImageGenerationCacheDecorator> _logger;
        private static readonly TimeSpan CacheDuration = TimeSpan.FromHours(6);

        public string ProviderName => _innerService.ProviderName;

        public AIImageGenerationCacheDecorator(
            IAIImageGenerationService innerService,
            IMemoryCache cache,
            ILogger<AIImageGenerationCacheDecorator> logger)
        {
            _innerService = innerService ?? throw new ArgumentNullException(nameof(innerService));
            _cache = cache ?? throw new ArgumentNullException(nameof(cache));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<GenerateImageResponse> GenerateImageAsync(GenerateImageRequest request, CancellationToken ct = default)
        {
            if (request == null)
            {
                throw new ArgumentNullException(nameof(request));
            }

            if (request.BypassCache)
            {
                _logger.LogInformation("Bypassing cache for image generation request.");
                var response = await _innerService.GenerateImageAsync(request, ct);
                if (response.IsSuccess)
                {
                    CacheResponse(request, response);
                }
                return response;
            }

            var cacheKey = GetCacheKey(request);
            if (_cache.TryGetValue(cacheKey, out GenerateImageResponse? cachedResponse) && cachedResponse != null)
            {
                _logger.LogInformation("Returning cached image generation response.");
                return cachedResponse;
            }

            _logger.LogInformation("Cache miss for image generation. Calling inner provider service...");
            var freshResponse = await _innerService.GenerateImageAsync(request, ct);

            if (freshResponse.IsSuccess)
            {
                CacheResponse(request, freshResponse);
            }

            return freshResponse;
        }

        public async Task<GenerateImageResponse> GenerateVariationsAsync(GenerateImageRequest request, CancellationToken ct = default)
        {
            if (request == null)
            {
                throw new ArgumentNullException(nameof(request));
            }

            if (request.BypassCache)
            {
                var response = await _innerService.GenerateVariationsAsync(request, ct);
                if (response.IsSuccess)
                {
                    CacheResponse(request, response);
                }
                return response;
            }

            var cacheKey = "VAR_" + GetCacheKey(request);
            if (_cache.TryGetValue(cacheKey, out GenerateImageResponse? cachedResponse) && cachedResponse != null)
            {
                _logger.LogInformation("Returning cached image variations response.");
                return cachedResponse;
            }

            var freshResponse = await _innerService.GenerateVariationsAsync(request, ct);
            if (freshResponse.IsSuccess)
            {
                _cache.Set(cacheKey, freshResponse, CacheDuration);
            }

            return freshResponse;
        }

        public async Task<GenerateImageResponse> RefineImageAsync(GenerateImageRequest request, CancellationToken ct = default)
        {
            if (request == null)
            {
                throw new ArgumentNullException(nameof(request));
            }

            if (request.BypassCache)
            {
                var response = await _innerService.RefineImageAsync(request, ct);
                if (response.IsSuccess)
                {
                    CacheResponse(request, response);
                }
                return response;
            }

            var cacheKey = "REF_" + GetCacheKey(request);
            if (_cache.TryGetValue(cacheKey, out GenerateImageResponse? cachedResponse) && cachedResponse != null)
            {
                _logger.LogInformation("Returning cached refined image response.");
                return cachedResponse;
            }

            var freshResponse = await _innerService.RefineImageAsync(request, ct);
            if (freshResponse.IsSuccess)
            {
                _cache.Set(cacheKey, freshResponse, CacheDuration);
            }

            return freshResponse;
        }

        public Task<bool> ValidateLimitsAsync(string userId, CancellationToken ct = default)
        {
            return _innerService.ValidateLimitsAsync(userId, ct);
        }

        public Task<AIHealthCheckResult> CheckHealthAsync(CancellationToken ct = default)
        {
            return _innerService.CheckHealthAsync(ct);
        }

        private void CacheResponse(GenerateImageRequest request, GenerateImageResponse response)
        {
            var cacheKey = GetCacheKey(request);
            _cache.Set(cacheKey, response, CacheDuration);
            _logger.LogInformation("Cached AI image response for {DurationHours} hours.", CacheDuration.TotalHours);
        }

        private string GetCacheKey(GenerateImageRequest request)
        {
            var input = $"{request.Prompt}|{request.NegativePrompt}|{request.ImageCount}|{request.BaseImageUrl}|{request.SimilarityWeight}";
            using var sha256 = SHA256.Create();
            var bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(input));
            return "AI_GEN_" + Convert.ToBase64String(bytes);
        }
    }
}
