using HandoraDomain.Models.ChatEntities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HandoraDomain.Interfaces
{
    public interface IChatRepository
    {
        Task<Conversation?> GetConversationAsync(string buyerId, string sellerId, CancellationToken ct = default);
        Task<Conversation?> GetConversationByIdAsync(Guid id, CancellationToken ct = default);
        Task<IReadOnlyList<Conversation>> GetUserConversationsAsync(string userId, CancellationToken ct = default);
        Task<IReadOnlyList<Message>> GetMessagesAsync(Guid conversationId, CancellationToken ct = default);
        Task<int> GetUnreadCountAsync(Guid conversationId, string userId, CancellationToken ct = default);
        Task AddConversationAsync(Conversation conversation, CancellationToken ct = default);
        Task AddMessageAsync(Message message, CancellationToken ct = default);
        Task MarkMessagesAsReadAsync(Guid conversationId, string userId, CancellationToken ct = default);
        Task<bool> SaveChangesAsync(CancellationToken ct = default);
    }
}
