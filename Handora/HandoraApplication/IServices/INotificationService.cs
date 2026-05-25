using HandoraApplication.DTOs.NotificationsDto;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HandoraApplication.IServices
{
    public interface INotificationService
    {
        Task SendAsync(SendNotificationDto dto, CancellationToken ct = default);
        Task<IReadOnlyList<NotificationDto>> GetUserNotificationsAsync(
            string userId, CancellationToken ct = default);
        Task<int> GetUnreadCountAsync(string userId, CancellationToken ct = default);
        Task MarkAsReadAsync(Guid notificationId, CancellationToken ct = default);
        Task MarkAllAsReadAsync(string userId, CancellationToken ct = default);
    }
}
