using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HandoraApplication.DTOs.ChatDTOs
{
    public sealed record ConversationDto
    {
        public Guid Id { get; init; }
        public string BuyerId { get; init; } = string.Empty;
        public string BuyerName { get; init; } = string.Empty;
        public string? BuyerImage { get; init; }
        public string SellerId { get; init; } = string.Empty;
        public string SellerName { get; init; } = string.Empty;
        public string? SellerImage { get; init; }
        public MessageDto? LastMessage { get; init; }
        public int UnreadCount { get; init; }
        public Guid? CustomRequestId { get; init; }
        public DateTime CreatedAt { get; init; }
    }
}
