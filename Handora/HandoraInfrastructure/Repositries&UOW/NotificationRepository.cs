using HandoraDomain.Interfaces;
using HandoraDomain.Models.NotificationEntities;
using HandoraInfrastructure.Data;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HandoraInfrastructure.Repositries_UOW
{
    public sealed class NotificationRepository : INotificationRepository
    {
        private readonly AppDbContext _context;

        public NotificationRepository(AppDbContext context)
            => _context = context;

        public async Task<IReadOnlyList<Notification>> GetUserNotificationsAsync(
            string userId, CancellationToken ct = default)
            => await _context.Notifications
                .Where(n => n.UserId == userId && n.Type != NotificationType.Message)
                .OrderByDescending(n => n.CreatedAt)
                .AsNoTracking()
                .ToListAsync(ct);

        public async Task<(IReadOnlyList<Notification> Items, int TotalCount)> GetUserNotificationsPagedAsync(
            string userId, int pageNumber, int pageSize, CancellationToken ct = default)
        {
            var query = _context.Notifications
                .Where(n => n.UserId == userId && n.Type != NotificationType.Message);

            var totalCount = await query.CountAsync(ct);

            var items = await query
                .OrderByDescending(n => n.CreatedAt)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .AsNoTracking()
                .ToListAsync(ct);

            return (items, totalCount);
        }

        public async Task<int> GetUnreadCountAsync(string userId, CancellationToken ct = default)
            => await _context.Notifications
                .CountAsync(n => n.UserId == userId && !n.IsRead && n.Type != NotificationType.Message, ct);

        public async Task<Notification?> GetByIdAsync(Guid id, CancellationToken ct = default)
            => await _context.Notifications.FindAsync([id], ct);

        public async Task AddAsync(Notification notification, CancellationToken ct = default)
            => await _context.Notifications.AddAsync(notification, ct);

        public async Task MarkAsReadAsync(Guid id, CancellationToken ct = default)
        {
            await _context.Notifications
                .Where(n => n.Id == id)
                .ExecuteUpdateAsync(s => s.SetProperty(n => n.IsRead, true), ct);
        }

        public async Task MarkAllAsReadAsync(string userId, CancellationToken ct = default)
        {
            await _context.Notifications
                .Where(n => n.UserId == userId && !n.IsRead && n.Type != NotificationType.Message)
                .ExecuteUpdateAsync(s => s.SetProperty(n => n.IsRead, true), ct);
        }

        public async Task<bool> SaveChangesAsync(CancellationToken ct = default)
            => await _context.SaveChangesAsync(ct) > 0;
    }
}
