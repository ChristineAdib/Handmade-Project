using HandoraDomain.Consts;
using HandoraApplication.DTOs.CustomStudioDTOs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HandoraApplication.DTOs.ChatDTOs
{
    public sealed record MessageDto
    {
        public Guid Id { get; init; }
        public string Content { get; init; } = string.Empty;
        public MessageType Type { get; init; }
        public string? ImageUrl { get; init; }
        public bool IsRead { get; init; }
        public string SenderId { get; init; } = string.Empty;
        public string SenderName { get; init; } = string.Empty;
        public Guid ConversationId { get; init; }
        public DateTime CreatedAt { get; init; }
        public CustomOfferDto? CustomOffer { get; init; }
    }
}
