using HandoraApplication.AI.DTOs;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace HandoraApplication.AI.Interfaces
{
    public interface IGiftConversationManager
    {
        Task<GiftRequestState> GetStateAsync(string sessionId);
        Task SaveStateAsync(string sessionId, GiftRequestState state);
        Task ClearStateAsync(string sessionId);

        Task<List<ChatHistoryEntry>> GetHistoryAsync(string sessionId);
        Task SaveHistoryAsync(string sessionId, List<ChatHistoryEntry> history);
    }

    public class ChatHistoryEntry
    {
        public string Role { get; set; } = string.Empty;   // "user" or "assistant"
        public string Content { get; set; } = string.Empty;
    }
}
