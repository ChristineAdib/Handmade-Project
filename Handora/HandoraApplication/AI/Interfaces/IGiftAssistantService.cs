using HandoraApplication.AI.DTOs;
using System.Threading.Tasks;

namespace HandoraApplication.AI.Interfaces
{
    public interface IGiftAssistantService
    {
        Task<GiftChatResponseDto> ProcessChatAsync(GiftChatRequestDto request);
        Task ResetSessionAsync(string sessionId);
    }
}
