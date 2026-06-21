using HandoraApplication.DTOs.ChatDTOs;
using HandoraApplication.Helpers;
using HandoraApplication.Helpers.AuthHelper;
using HandoraApplication.Hubs;
using HandoraApplication.IServices;
using HandoraDomain.Interfaces;
using HandoraDomain.Models.ChatEntities;
using HandoraDomain.Models.ShopEntities;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using HandoraDomain.Models.NotificationEntities;
using HandoraApplication.DTOs.NotificationsDto;

namespace HandoraApplication.Services
{

    public sealed class ChatService : IChatService
    {
        private readonly IChatRepository _chatRepo;
        private readonly IChatHubContext _hubContext; 
        private readonly ILogger<ChatService> _logger;
        private readonly IUnitOfWork _unitOfWork;
        private readonly INotificationService _notificationService;

        public ChatService(
            IChatRepository chatRepo,
            IChatHubContext hubContext, 
            ILogger<ChatService> logger,
            IUnitOfWork unitOfWork,
            INotificationService notificationService)
        {
            _chatRepo = chatRepo;
            _hubContext = hubContext;
            _logger = logger;
            _unitOfWork = unitOfWork;
            _notificationService = notificationService;
        }

        public async Task<ConversationDto> StartConversationByShopAsync(
            string buyerId, Guid shopId, CancellationToken ct = default)
        {
            var shop = await _unitOfWork.Repository<Shop, Guid>().GetByIdAsync(shopId);
            if (shop is null)
                throw new KeyNotFoundException("Shop not found");

            return await StartConversationAsync(buyerId, shop.OwnerId, ct);
        }

        public async Task<ConversationDto> StartConversationAsync(
    string buyerId, string sellerId, CancellationToken ct = default)
        {
            if (buyerId == sellerId)
                throw new AuthException("You cannot send messages to yourself.");

            var existing = await _chatRepo.GetConversationAsync(buyerId, sellerId, ct);
            if (existing is not null)
                return await MapConversationAsync(existing, buyerId, ct);

            var conversation = new Conversation
            {
                Id = Guid.NewGuid(),
                BuyerId = buyerId,
                SellerId = sellerId,
                CreatedAt = DateTime.UtcNow
            };

            await _chatRepo.AddConversationAsync(conversation, ct);
            await _chatRepo.SaveChangesAsync(ct);

            var created = await _chatRepo.GetConversationByIdAsync(conversation.Id, ct);
            var dto = await MapConversationAsync(created!, buyerId, ct);

            await _hubContext.ConversationStartedAsync(sellerId, dto);

            return dto;
        }

        public async Task<IReadOnlyList<ConversationDto>> GetUserConversationsAsync(
            string userId, CancellationToken ct = default)
        {
            var conversations = await _chatRepo.GetUserConversationsAsync(userId, ct);
            var result = new List<ConversationDto>();

            foreach (var c in conversations)
                result.Add(await MapConversationAsync(c, userId, ct));

            return result;
        }

        public async Task<IReadOnlyList<MessageDto>> GetMessagesAsync(
            Guid conversationId, string userId, CancellationToken ct = default)
        {
            // تأكد إن اليوزر ده طرف في المحادثة
            var conversation = await _chatRepo.GetConversationByIdAsync(conversationId, ct);
            if (conversation is null || (conversation.BuyerId != userId && conversation.SellerId != userId))
                throw new AuthException("Unauthorized access to this conversation.");

            var messages = await _chatRepo.GetMessagesAsync(conversationId, ct);
            return messages.Select(MapMessage).ToList();
        }

        public async Task<MessageDto> SendMessageAsync(
            string senderId, SendMessageDto dto, CancellationToken ct = default)
        {
            var conversation = await _chatRepo.GetConversationByIdAsync(dto.ConversationId, ct);

            if (conversation is null ||
                (conversation.BuyerId != senderId && conversation.SellerId != senderId))
                throw new AuthException("Unauthorized access to this conversation.");

            if (conversation.BuyerId == conversation.SellerId)
                throw new AuthException("You cannot send messages to yourself.");

            if (ChatValidationHelper.ContainsPhoneNumber(dto.Content))
                throw new AuthException("Phone numbers are not allowed in chat.");

            if (ChatValidationHelper.ContainsLinks(dto.Content))
                throw new AuthException("Links are not allowed in chat.");

            var message = new Message
            {
                Id = Guid.NewGuid(),
                Content = dto.Content,
                Type = dto.Type,
                ImageUrl = dto.ImageUrl,
                SenderId = senderId,
                ConversationId = dto.ConversationId,
                CreatedAt = DateTime.UtcNow
            };

            await _chatRepo.AddMessageAsync(message, ct);
            await _chatRepo.SaveChangesAsync(ct);

            // reload with Sender
            var messages = await _chatRepo.GetMessagesAsync(dto.ConversationId, ct);
            var saved = messages.Last();
            var msgDto = MapMessage(saved);

            // ابعت للطرف التاني real-time
            var receiverId = conversation.BuyerId == senderId
                ? conversation.SellerId
                : conversation.BuyerId;

            await _hubContext.SendMessageAsync(receiverId, msgDto);

            _logger.LogInformation(
                "Message sent in conversation {ConvId} by {SenderId}", dto.ConversationId, senderId);

            return msgDto;
        }

        public async Task MarkAsReadAsync(
            Guid conversationId, string userId, CancellationToken ct = default)
        {
            await _chatRepo.MarkMessagesAsReadAsync(conversationId, userId, ct);

            // أبلغ الطرف التاني إن الرسايل اتقرأت
            var conversation = await _chatRepo.GetConversationByIdAsync(conversationId, ct);
            if (conversation is null) return;

            var otherUserId = conversation.BuyerId == userId
                ? conversation.SellerId
                : conversation.BuyerId;

            await _hubContext.MessagesReadAsync(otherUserId, conversationId);
        }


        private async Task<ConversationDto> MapConversationAsync(
            Conversation c, string currentUserId, CancellationToken ct)
        {
            var lastMessage = c.Messages.OrderByDescending(m => m.CreatedAt).FirstOrDefault();
            var unreadCount = await _chatRepo.GetUnreadCountAsync(c.Id, currentUserId, ct);

            return new ConversationDto
            {
                Id = c.Id,
                BuyerId = c.BuyerId,
                BuyerName = c.Buyer.Name,
                BuyerImage = c.Buyer.ProfileImage,
                SellerId = c.SellerId,
                SellerName = c.Seller.Name,
                SellerImage = c.Seller.ProfileImage,
                LastMessage = lastMessage is null ? null : MapMessage(lastMessage),
                UnreadCount = unreadCount,
                CreatedAt = c.CreatedAt
            };
        }

        private static MessageDto MapMessage(Message m) => new()
        {
            Id = m.Id,
            Content = m.Content,
            Type = m.Type,
            ImageUrl = m.ImageUrl,
            IsRead = m.IsRead,
            SenderId = m.SenderId,
            SenderName = m.Sender?.Name ?? string.Empty,
            ConversationId = m.ConversationId,
            CreatedAt = m.CreatedAt
        };
    }
}
