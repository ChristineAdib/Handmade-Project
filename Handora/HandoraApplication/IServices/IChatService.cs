using HandoraApplication.DTOs.ChatDTOs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HandoraApplication.IServices
{
    public interface IChatService
    {
        Task<ConversationDto> StartConversationAsync(string buyerId, string sellerId, CancellationToken ct = default);
        Task<IReadOnlyList<ConversationDto>> GetUserConversationsAsync(string userId, CancellationToken ct = default);
        Task<IReadOnlyList<MessageDto>> GetMessagesAsync(Guid conversationId, string userId, CancellationToken ct = default);
        Task<MessageDto> SendMessageAsync(string senderId, SendMessageDto dto, CancellationToken ct = default);
        Task MarkAsReadAsync(Guid conversationId, string userId, CancellationToken ct = default);
    }
}
