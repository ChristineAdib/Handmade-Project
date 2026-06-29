using System.Threading;
using System.Threading.Tasks;
using HandoraApplication.AI.DTOs;

namespace HandoraApplication.AI.Interfaces
{
    public interface IGenerationQualityValidator
    {
        Task<QualityValidationResult> ValidateAsync(byte[] imageData, string prompt, CancellationToken ct = default);
    }
}
