using HandoraDomain.Interfaces;
using HandoraDomain.Models.ChatEntities;
using HandoraInfrastructure.Data;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HandoraInfrastructure.Repositries_UOW
{
    public sealed class ChatRepository : IChatRepository
    {
        private readonly AppDbContext _context;

        public ChatRepository(AppDbContext context)
            => _context = context;

        public async Task<Conversation?> GetConversationAsync(
            string buyerId, string sellerId, CancellationToken ct = default)
            => await _context.Conversations
                .Include(c => c.Buyer)
                .Include(c => c.Seller)
                .FirstOrDefaultAsync(c =>
                    c.BuyerId == buyerId &&
                    c.SellerId == sellerId &&
                    !c.IsDeleted, ct);

        public async Task<Conversation?> GetConversationByIdAsync(
            Guid id, CancellationToken ct = default)
            => await _context.Conversations
                .Include(c => c.Buyer)
                .Include(c => c.Seller)
                .FirstOrDefaultAsync(c => c.Id == id && !c.IsDeleted, ct);

        public async Task<IReadOnlyList<Conversation>> GetUserConversationsAsync(
            string userId, CancellationToken ct = default)
            => await _context.Conversations
                .Include(c => c.Buyer)
                .Include(c => c.Seller)
                .Include(c => c.Messages.OrderByDescending(m => m.CreatedAt).Take(1))
                    .ThenInclude(m => m.Sender)
                .Where(c => (c.BuyerId == userId || c.SellerId == userId) && !c.IsDeleted)
                .OrderByDescending(c => c.Messages.Max(m => (DateTime?)m.CreatedAt) ?? c.CreatedAt)
                .AsNoTracking()
                .ToListAsync(ct);

        public async Task<IReadOnlyList<Message>> GetMessagesAsync(
            Guid conversationId, CancellationToken ct = default)
            => await _context.Messages
                .Include(m => m.Sender)
                .Where(m => m.ConversationId == conversationId && !m.IsDeleted)
                .OrderBy(m => m.CreatedAt)
                .AsNoTracking()
                .ToListAsync(ct);

        public async Task<int> GetUnreadCountAsync(
            Guid conversationId, string userId, CancellationToken ct = default)
            => await _context.Messages
                .CountAsync(m =>
                    m.ConversationId == conversationId &&
                    m.SenderId != userId &&
                    !m.IsRead, ct);

        public async Task AddConversationAsync(
            Conversation conversation, CancellationToken ct = default)
            => await _context.Conversations.AddAsync(conversation, ct);

        public async Task AddMessageAsync(
            Message message, CancellationToken ct = default)
            => await _context.Messages.AddAsync(message, ct);

        public async Task MarkMessagesAsReadAsync(
            Guid conversationId, string userId, CancellationToken ct = default)
            => await _context.Messages
                .Where(m =>
                    m.ConversationId == conversationId &&
                    m.SenderId != userId &&
                    !m.IsRead)
                .ExecuteUpdateAsync(s => s.SetProperty(m => m.IsRead, true), ct);

        public async Task<bool> SaveChangesAsync(CancellationToken ct = default)
            => await _context.SaveChangesAsync(ct) > 0;
    }
}
