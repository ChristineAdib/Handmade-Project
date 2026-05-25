using HandoraDomain.Consts;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HandoraApplication.DTOs.ChatDTOs
{
    public sealed record SendMessageDto
    {
        public Guid ConversationId { get; init; }

        public string Content { get; init; } = string.Empty;

        public MessageType Type { get; init; } = MessageType.Text;

        public string? ImageUrl { get; init; }
    }
}
