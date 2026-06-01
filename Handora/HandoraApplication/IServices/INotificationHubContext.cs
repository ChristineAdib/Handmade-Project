using HandoraApplication.DTOs.NotificationsDto;

namespace HandoraApplication.IServices;

public interface INotificationHubContext
    {
        Task SendNotificationAsync(string userId, NotificationDto notification);
        Task SendUnreadCountAsync(string userId, int count);
    }
