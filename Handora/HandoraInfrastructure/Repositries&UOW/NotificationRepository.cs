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
                .Where(n => n.UserId == userId)
                .OrderByDescending(n => n.CreatedAt)
                .AsNoTracking()
                .ToListAsync(ct);

        public async Task<int> GetUnreadCountAsync(string userId, CancellationToken ct = default)
            => await _context.Notifications
                .CountAsync(n => n.UserId == userId && !n.IsRead, ct);

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
                .Where(n => n.UserId == userId && !n.IsRead)
                .ExecuteUpdateAsync(s => s.SetProperty(n => n.IsRead, true), ct);
        }

        public async Task<bool> SaveChangesAsync(CancellationToken ct = default)
            => await _context.SaveChangesAsync(ct) > 0;
    }
}
