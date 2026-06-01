using HandoraApplication.DTOs.ChatDTOs;
using HandoraApplication.Hubs;
using Microsoft.AspNetCore.SignalR;

namespace HandoraApi.Hubs
{
    public sealed class ChatHubContext : IChatHubContext
    {
        private readonly IHubContext<ChatHub, IChatHub> _hubContext;

        public ChatHubContext(IHubContext<ChatHub, IChatHub> hubContext)
            => _hubContext = hubContext;

        public Task SendMessageAsync(string userId, MessageDto message)
            => _hubContext.Clients.Group(userId).ReceiveMessage(message);

        public Task ConversationStartedAsync(string userId, ConversationDto conversation)
            => _hubContext.Clients.Group(userId).ConversationStarted(conversation);

        public Task MessagesReadAsync(string userId, Guid conversationId)
            => _hubContext.Clients.Group(userId).MessagesRead(conversationId);
    }
}
