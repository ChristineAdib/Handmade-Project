using HandoraApplication.DTOs.ChatDTOs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HandoraDomain.Interfaces
{
    public interface IChatHub
    {
        Task ReceiveMessage(MessageDto message);
        Task ConversationStarted(ConversationDto conversation);
        Task MessagesRead(Guid conversationId);
    }
}
