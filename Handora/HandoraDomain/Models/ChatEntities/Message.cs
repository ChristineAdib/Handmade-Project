using HandoraDomain.Consts;
using HandoraDomain.Models.AppUser;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HandoraDomain.Models.ChatEntities
{

    public class Message : BaseEntity<Guid>
    {
        public string Content { get; set; } = string.Empty;
        public MessageType Type { get; set; } = MessageType.Text;
        public string? ImageUrl { get; set; }         // لو Type == Image
        public bool IsRead { get; set; } = false;

        public string SenderId { get; set; } = string.Empty;
        public User Sender { get; set; } = null!;

        public Guid ConversationId { get; set; }
        public Conversation Conversation { get; set; } = null!;
    }
}
