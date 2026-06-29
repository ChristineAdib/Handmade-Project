using System.Threading;
using System.Threading.Tasks;
using HandoraApplication.AI.DTOs;

namespace HandoraApplication.AI.Interfaces
{
    public interface IAIImageGenerationService
    {
        string ProviderName { get; }
        Task<GenerateImageResponse> GenerateImageAsync(GenerateImageRequest request, CancellationToken ct = default);
        Task<GenerateImageResponse> GenerateVariationsAsync(GenerateImageRequest request, CancellationToken ct = default);
        Task<GenerateImageResponse> RefineImageAsync(GenerateImageRequest request, CancellationToken ct = default);
        Task<bool> ValidateLimitsAsync(string userId, CancellationToken ct = default);
        Task<AIHealthCheckResult> CheckHealthAsync(CancellationToken ct = default);
    }
}
