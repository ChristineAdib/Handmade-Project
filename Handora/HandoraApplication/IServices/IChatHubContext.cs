using HandoraApplication.DTOs.ChatDTOs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HandoraApplication.IServices
{
    public interface IChatHubContext
    {
        Task SendMessageAsync(string userId, MessageDto message);
        Task ConversationStartedAsync(string userId, ConversationDto conversation);
        Task MessagesReadAsync(string userId, Guid conversationId);
    }
}
