using HandoraDomain.Models.NotificationEntities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HandoraDomain.Interfaces
{
    public interface INotificationRepository
    {
        Task<IReadOnlyList<Notification>> GetUserNotificationsAsync(
            string userId, CancellationToken ct = default);

        Task<(IReadOnlyList<Notification> Items, int TotalCount)> GetUserNotificationsPagedAsync(
            string userId, int pageNumber, int pageSize, CancellationToken ct = default);

        Task<int> GetUnreadCountAsync(string userId, CancellationToken ct = default);
        Task<Notification?> GetByIdAsync(Guid id, CancellationToken ct = default);
        Task AddAsync(Notification notification, CancellationToken ct = default);
        Task MarkAsReadAsync(Guid id, CancellationToken ct = default);
        Task MarkAllAsReadAsync(string userId, CancellationToken ct = default);
        // INotificationRepository.cs
        Task<bool> SaveChangesAsync(CancellationToken ct = default);
    }
}
